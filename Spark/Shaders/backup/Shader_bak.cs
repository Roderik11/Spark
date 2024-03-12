using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX.D3DCompiler;
using System.Reflection;

namespace Spark
{
    public sealed class ShaderDescription2
    {
        public string Filename = string.Empty;
        public string VsEntry = string.Empty;
        public string PsEntry = string.Empty;
        public string HsEntry = string.Empty;
        public string DsEntry = string.Empty;

        public InputElement[] Elements;
        public int Technique;

        public override bool Equals(object obj)
        {
            if (obj is ShaderDescription2)
            {
                ShaderDescription2 other = obj as ShaderDescription2;
                return other.GetHashCode() == GetHashCode();
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return string.Format("{0}{1}{2}{3}{4}{5}", Filename, VsEntry, PsEntry, HsEntry, DsEntry, Technique).GetHashCode();
        }
    }

    public class Effect2
    {
        public string Name;
        public InputLayout Layout;
        public int Technique = 0;
        public BlendState BlendState;
        public DepthStencilState DepthStencilState;
        public RasterizerState RasterizerState;
        public ShaderDescription Description { get; private set; }
        public RenderPass RenderPass { get; set; }

        protected Dictionary<string, EffectParameter> Properties = new Dictionary<string, EffectParameter>();
        protected Shader Shader;

        private Dictionary<int, InputLayout> inputLayouts = new Dictionary<int, InputLayout>();

        public Effect2(string fileName)
        {
            Shader = Shader.Create(new ShaderDescription2 { Filename = fileName, VsEntry = "VS", PsEntry = "PS" });
        }

        public void SetParameter<T>(string name, T value)
        {
            if (Shader == null) return;
            Shader.SetParameter(name, value);
        }

        public void Apply()
        {
            ApplyStates();
            ApplyParameters();

            Profiler.Start("Effect: Pass");
            Shader.Apply();
            Profiler.Stop();
        }

        private void ApplyStates()
        {
            Profiler.Start("Effect: States");

            if (RasterizerState != null)
                Engine.Device.ImmediateContext.Rasterizer.State = RasterizerState;

            if (BlendState != null)
                Engine.Device.ImmediateContext.OutputMerger.BlendState = BlendState;

            if (DepthStencilState != null)
                Engine.Device.ImmediateContext.OutputMerger.DepthStencilState = DepthStencilState;

            Profiler.Stop();
        }

        private void ApplyParameters()
        {
            Profiler.Start("Effect: Params");

            foreach (var pair in Properties)
                pair.Value.Apply();

            Profiler.Stop();
        }
    }

    public class Shader
    {
        public delegate bool BoolAction<T>(string name, T value);

        public InputLayout Layout { get; private set; }
        public VertexShader VertexShader { get; private set; }
        public PixelShader PixelShader { get; private set; }
        public HullShader HullShader { get; private set; }
        public DomainShader DomainShader { get; private set; }
        public GeometryShader GeometryShader { get; private set; }
        public ShaderDescription2 Description { get; private set; }

        private Dictionary<string, ShaderParameter> Parameters = new Dictionary<string, ShaderParameter>();
        private Dictionary<string, ConstantBufferBox> VSBoxes = new Dictionary<string, ConstantBufferBox>();
        private Dictionary<string, ConstantBufferBox> PSBoxes = new Dictionary<string, ConstantBufferBox>();
        private Dictionary<string, EffectTextureParameter> texParams = new Dictionary<string, EffectTextureParameter>();

        public static Dictionary<int, Shader> Masters = new Dictionary<int, Shader>();

        private MethodInfo StructMethod = typeof(Shader).GetMethod("SetStruct", BindingFlags.NonPublic | BindingFlags.Instance);
        private MethodInfo ClassMethod = typeof(Shader).GetMethod("SetClass", BindingFlags.NonPublic | BindingFlags.Instance);
        private Dictionary<Type, MethodInfo> TypeDelegate = new Dictionary<Type, MethodInfo>();

        public static Shader Create(ShaderDescription2 desc)
        {
            int hash = desc.GetHashCode();

            if (!Masters.ContainsKey(hash))
            {
                Shader shader = new Shader(desc);
                Masters.Add(hash, shader);
            }

            return Masters[hash];
        }

        private Shader(ShaderDescription2 desc)
        {
            Description = desc;

            object vs = Properties.Resources.ResourceManager.GetObject(Description.Filename);
            if (vs == null) return;

            byte[] data = (byte[])vs;

            string errors = string.Empty;

            try
            {
                Include lnc = new ShaderInclude();
                
                using (var bytecode = ShaderBytecode.Compile(data, Description.VsEntry, "vs_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None, null, lnc, ""))
                {
                    VertexShader = new VertexShader(Engine.Device, bytecode);
                    ShaderSignature Signature = ShaderSignature.GetInputSignature(bytecode);
                   // Layout = new InputLayout(Engine.Device, Signature, Description.Elements);

                    using (var psbytecode = ShaderBytecode.Compile(data, Description.PsEntry, "ps_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None, null, lnc, ""))
                    {
                        PixelShader = new PixelShader(Engine.Device, psbytecode);
                        Reflection(bytecode, psbytecode);
                    }
                }

                if (!string.IsNullOrEmpty(Description.HsEntry))
                {
                    using (var hsbytecode = ShaderBytecode.Compile(data, Description.HsEntry, "hs_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None, null, lnc, ""))
                    {
                        HullShader = new HullShader(Engine.Device, hsbytecode);
                    }

                    using (var dsbytecode = ShaderBytecode.Compile(data, Description.DsEntry, "ds_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None, null, lnc, ""))
                    {
                        DomainShader = new DomainShader(Engine.Device, dsbytecode);
                    }
                }
                // output warnings
                System.Diagnostics.Debug.Write(errors);
            }
            catch
            {
                throw new Exception(errors);
            }
        }

        //public ShaderParameters CreateParameters()
        //{
        //    ShaderParameters result = new ShaderParameters();
        //    foreach (ShaderParameter param in Parameters.Values)
        //        result.Add(param.Clone());
        //    return result;
        //}

        //public void Bind(ShaderParameters parameters)
        //{
        //    foreach (ShaderParameter param in parameters)
        //    {
        //        if (!param.IsDirty) continue;

        //        SetParameter(param.Name, param.Value);
        //        param.IsDirty = false;
        //    }
        //}

        private void Reflection(ShaderBytecode vertexshader, ShaderBytecode pixelshader)
        {
            ShaderReflection reflect = new ShaderReflection(vertexshader);

            for (int i = 0; i < reflect.Description.ConstantBuffers; i++)
            {
                ConstantBuffer buffer = reflect.GetConstantBuffer(i);
             
                VSBoxes.Add(buffer.Description.Name, new ConstantBufferBox(buffer));

                for (int j = 0; j < buffer.Description.VariableCount; j++)
                {
                    ShaderReflectionVariable variable = buffer.GetVariable(j);

                    ShaderParameter param = new ShaderParameter
                    {
                        Name = variable.Description.Name,
                        Type = TranslateVariable(variable)
                    };

                    Parameters.Add(param.Name, param);
                }
            }

            for (int i = 0; i < reflect.Description.BoundResources; i++)
            {
                InputBindingDescription input = reflect.GetResourceBindingDescription(i);
                if (input.Type == ShaderInputType.Texture)
                {
                    if (Parameters.ContainsKey(input.Name)) continue;

                    EffectTextureParameter texparam = new EffectTextureParameter
                    {
                        Slot = input.BindPoint,
                        Count = input.BindCount,
                        Textures = new ShaderResourceView[input.BindCount]
                    };
                    texParams.Add(input.Name, texparam);

                    ShaderParameter param = new ShaderParameter
                    {
                        Name = input.Name,
                        Type = typeof(Texture[])
                    };

                    Parameters.Add(param.Name, param);
                }
            }

            ShaderReflection reflect1 = new ShaderReflection(pixelshader);

            for (int i = 0; i < reflect1.Description.ConstantBuffers; i++)
            {
                ConstantBuffer buffer = reflect1.GetConstantBuffer(i);

                if (VSBoxes.ContainsKey(buffer.Description.Name))
                    PSBoxes.Add(buffer.Description.Name, VSBoxes[buffer.Description.Name]);
                else
                {
                    PSBoxes.Add(buffer.Description.Name, new ConstantBufferBox(buffer));

                    for (int j = 0; j < buffer.Description.VariableCount; j++)
                    {
                        ShaderReflectionVariable variable = buffer.GetVariable(j);

                        ShaderParameter param = new ShaderParameter
                        {
                            Name = variable.Description.Name,
                            Type = TranslateVariable(variable)
                        };

                        Parameters.Add(param.Name, param);
                    }
                }
            }

            for (int i = 0; i < reflect1.Description.BoundResources; i++)
            {
                InputBindingDescription input = reflect1.GetResourceBindingDescription(i);
               
                if (input.Type == ShaderInputType.Texture)
                {
                    if (Parameters.ContainsKey(input.Name)) continue;

                    EffectTextureParameter texparam = new EffectTextureParameter
                    {
                        Slot = input.BindPoint,
                        Count = input.BindCount,
                        Textures = new ShaderResourceView[input.BindCount]
                    };
                    texParams.Add(input.Name, texparam);

                    ShaderParameter param = new ShaderParameter
                    {
                        Name = input.Name,
                        Type = typeof(Texture[])
                    };

                    Parameters.Add(param.Name, param);
                }

                if(input.Type == ShaderInputType.Sampler)
                {

                }
            }
        }

        private Type TranslateVariable(ShaderReflectionVariable var)
        {
            ShaderTypeDescription type = var.GetVariableType().Description;

            if (type.Class == ShaderVariableClass.MatrixColumns)
            {
                if (type.ElementCount > 1)
                    return typeof(Matrix[]);

                return typeof(Matrix);
            }

            if (type.Class == ShaderVariableClass.MatrixColumns)
                return typeof(Matrix);

            if (type.Class == ShaderVariableClass.Vector)
            {
                if (type.ColumnCount == 2)
                    return typeof(Vector2);
                if (type.ColumnCount == 3)
                    return typeof(Vector3);
                if (type.ColumnCount == 4)
                    return typeof(Vector4);
            }

            if (type.Class == ShaderVariableClass.Scalar)
            {
                if (type.Type == ShaderVariableType.Float)
                    return typeof(float);
                if (type.Type == ShaderVariableType.Bool)
                    return typeof(bool);
                if (type.Type == ShaderVariableType.Int)
                    return typeof(int);
            }

            return null;
        }

        public bool SetParameter<T>(string name, T value)
        {
            if (!Parameters.ContainsKey(name)) return false;

            Type type = Parameters[name].Type;

            if (!TypeDelegate.TryGetValue(type, out var action))
            {
                MethodInfo mi = type.IsValueType ? StructMethod : ClassMethod;
                MethodInfo gen = mi.MakeGenericMethod(type);

                //action = Delegate.CreateDelegate(typeof(BoolAction<>).MakeGenericType(type), this, gen);
                TypeDelegate.Add(type, gen);
                action = gen;
            }

            return (bool)action.Invoke(this, new object[2] { name, value });
            //return (bool)action.DynamicInvoke(name, value);
        }

        private bool SetStruct<T>(string name, T value) where T : struct
        {
            foreach (ConstantBufferBox box in VSBoxes.Values)
            {
                if (box.SetParameter(name, value))
                    return true;
            }

            foreach (ConstantBufferBox box in PSBoxes.Values)
            {
                if (box.SetParameter(name, value))
                    return true;
            }

            return false;
        }

        private bool SetClass<T>(string name, T value) where T : class
        {
            if (value is Texture[])
                return SetTexture(name, value as Texture[]);

            return false;
        }

        public bool SetTexture(string name, params Texture[] texture)
        {
            if (!texParams.ContainsKey(name)) return false;

            for (int i = 0; i < texParams[name].Count; i++)
            {
                if (texture.Length > i)
                    texParams[name].Textures[i] = texture[i]?.View;
            }

            return true;
        }

        public void Apply()
        {
            Engine.Device.ImmediateContext.InputAssembler.InputLayout = Layout;
            Engine.Device.ImmediateContext.VertexShader.Set(VertexShader);
            Engine.Device.ImmediateContext.PixelShader.Set(PixelShader);
            //Engine.Device.ImmediateContext.GeometryShader.Set(GeometryShader);

            if (HullShader != null)
            {
                Engine.Device.ImmediateContext.HullShader.Set(HullShader);
                Engine.Device.ImmediateContext.DomainShader.Set(DomainShader);
            }

            int i = 0;
            foreach (ConstantBufferBox box in VSBoxes.Values)
            {
                box.Commit();
                Engine.Device.ImmediateContext.VertexShader.SetConstantBuffer(i, box.Buffer);
                i++;
            }

            i = 0;
            foreach (ConstantBufferBox box in PSBoxes.Values)
            {
                box.Commit();
                Engine.Device.ImmediateContext.PixelShader.SetConstantBuffer(i, box.Buffer);
                i++;
            }

            foreach (EffectTextureParameter param in texParams.Values)
            {
                Engine.Device.ImmediateContext.PixelShader.SetShaderResources(param.Slot, param.Count, param.Textures);
            }
        }
    }
}