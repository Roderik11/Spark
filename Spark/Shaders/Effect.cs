using System;
using System.Collections.Generic;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

namespace Spark
{

    public class Effect : Asset
    {
        public int Pass = 0;
        public int Technique = 0;
        public InputLayout Layout;
        public BlendState BlendState;
        public DepthStencilState DepthStencilState;
        public RasterizerState RasterizerState;
        public ShaderDescription Description { get; private set; }

        private readonly Dictionary<int, InputLayout> inputLayouts = new Dictionary<int, InputLayout>();

        protected Dictionary<string, EffectParameter> Properties = new Dictionary<string, EffectParameter>();
        protected SharpDX.Direct3D11.Effect Shader;

        internal static Dictionary<int, Effect> MasterEffects = new Dictionary<int, Effect>();

        private EffectPass CurrentPass => Shader.GetTechniqueByIndex(Technique).GetPassByIndex(Pass);
        public IEnumerable<KeyValuePair<string, EffectParameter>> GetParams()
        {
            foreach (var pair in Properties)
                yield return pair;
        }

        private int _instanceId = _instanceCount++;
        private static volatile int _instanceCount;

        public int GetInstanceId() => _instanceId;

        private Effect()
        {
            RasterizerState = States.BackCull;
        }

        private Effect(SharpDX.Direct3D11.Effect shader) : this()
        {
            Shader = shader;
            GetParameters();
        }

        public Effect(string filename) : this()
        {
            Shader = CreateShader(filename);
            GetParameters();
        }

        public bool SetValue<T>(string name, T value)
        {
            if (Shader == null) return false;

            if (Properties.TryGetValue(name, out EffectParameter prop))
            {
                prop.Frequency = UpdateRate.PerObject;
                prop.SetValue(value);
                return true;
            }

            return false;
        }

        public void SetParameter<T>(string name, T value)
        {
            if (Shader == null) return;

            if (Properties.TryGetValue(name, out EffectParameter prop))
            {
                prop.SetValue(value);
                prop.Apply();
            }
        }

        public void Apply()
        {
            Profiler.Start("Effect: Apply");
            ApplyStates();
            ApplyParameters();
            Profiler.Stop();

            Profiler.Start("DXEffect: Apply");
            CurrentPass.Apply(Engine.Device.ImmediateContext);
            Profiler.Stop();
        }

        internal void ApplyStates()
        {
            Profiler.Start("Effect: States");

            if (RasterizerState != null && RasterizerState != Engine.Device.ImmediateContext.Rasterizer.State)
                Engine.Device.ImmediateContext.Rasterizer.State = RasterizerState;

            if (BlendState != null && BlendState != Engine.Device.ImmediateContext.OutputMerger.BlendState)
                Engine.Device.ImmediateContext.OutputMerger.BlendState = BlendState;

            if (DepthStencilState != null && DepthStencilState != Engine.Device.ImmediateContext.OutputMerger.DepthStencilState)
                Engine.Device.ImmediateContext.OutputMerger.DepthStencilState = DepthStencilState;

            Profiler.Stop();
        }

        internal void ApplyParameters()
        {
            Profiler.Start("Effect: Params");

            foreach (var pair in Properties)
                pair.Value.Apply();

            Profiler.Stop();
        }

        public InputLayout GetInputLayout(InputElement[] elements)
        {
            if (CurrentPass == null)
                return null;

            int hash = elements.GetHashCode();

            if (inputLayouts.TryGetValue(hash, out var result))
                return result;

            var desc = CurrentPass.VertexShaderDescription.Variable.GetShaderDescription(0);

            //for (int i = 0; i < desc.InputParameterCount; i++)
            //    var eld = currentPass.VertexShaderDescription.Variable.GetInputSignatureElementDescription(0, i);

            var signature = desc.Signature;
            result = new InputLayout(Engine.Device, signature, elements);

            if (result != null)
                inputLayouts.Add(hash, result);

            return result;
        }

        private static SharpDX.Direct3D11.Effect CreateShader(string filename)
        {
            int hash = filename.GetHashCode();
         
            if (MasterEffects.TryGetValue(hash, out var effect))
                return effect.Shader;

            string name = System.IO.Path.GetFileName(filename);
            string shortname = System.IO.Path.GetFileNameWithoutExtension(name);

            object resource = Spark.Properties.Resources.ResourceManager.GetObject(shortname);
            ShaderBytecode bytecode;
            string errors = string.Empty;

            if (resource != null)
            {
                byte[] data = (byte[])resource;
                bytecode = ShaderBytecode.Compile(data, "fx_5_0", ShaderFlags.None, EffectFlags.None, null, new ShaderInclude(), "");
            }
            else
            {
                string file = System.IO.File.ReadAllText(filename);
                bytecode = ShaderBytecode.Compile(file, "fx_5_0", ShaderFlags.None, EffectFlags.None, null, new ShaderInclude(), "");
            }

            SharpDX.Direct3D11.Effect shader = new SharpDX.Direct3D11.Effect(Engine.Device, bytecode);
            MasterEffects.Add(hash, new Effect(shader));
            return shader;
        }

        public void GetSubshaders(List<SubShader> result)
        {
            result.Clear();
            for (int i = 0; i < Shader.Description.TechniqueCount; i++)
            {
                var technique = Shader.GetTechniqueByIndex(i);
                result.Add(new SubShader(technique));
            }
        }

        private void GetParameters()
        {
            if (Shader == null) return;

            int count = Shader.Description.GlobalVariableCount;
            for (int i = 0; i < count; i++)
            {
                var effectVariable = Shader.GetVariableByIndex(i);
                
                var prop = effectVariable.ToEffectProperty();
                if (prop == null) continue;

                prop.Initialize(effectVariable);
                var annotation = effectVariable.GetAnnotationByName("Property");
                prop.Annotated = annotation.IsValid;
                Properties.Add(effectVariable.Description.Name, prop);
            }
        }

    }
}