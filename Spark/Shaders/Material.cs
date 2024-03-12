using SharpDX.Direct3D11;
using SharpDX.WIC;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Spark
{
    public class ShaderPass
    {
        public RenderPass RenderPass;
        public EffectPass EffectPass;
    }

    public class SubShader
    {
        public List<ShaderPass> ShaderPasses = new List<ShaderPass>();

        public SubShader(EffectTechnique technique)
        {
            for (int i = 0; i < technique.Description.PassCount; i++)
            {
                var pass = technique.GetPassByIndex(i);
                var name = pass.Description.Name;
                
                Enum.TryParse<RenderPass>(name, out var type);

                ShaderPasses.Add(new ShaderPass
                {
                    EffectPass = pass,
                    RenderPass = type,
                });
            }
        }
    }

    public class Material : Asset
    {
        public Effect Effect { get; internal set; }

        public int Technique
        {
            get => Effect.Technique;
            set => Effect.Technique = value;
        }

        public int Pass
        {
            get => Effect.Pass;
            set => Effect.Pass = value;
        }

        public bool UseInstancing = false;
        
        private readonly List<SubShader> SubShaders = new List<SubShader>();
        private EffectPass activePass;

        public List<ShaderPass> GetPasses() => SubShaders[Technique].ShaderPasses;

        public void SetPass(RenderPass pass)
        {
            int i = 0;
            foreach (var p in GetPasses())
            {
                if (p.RenderPass == pass)
                {
                    Effect.Pass = i;
                    activePass = p.EffectPass;
                    break;
                }
                i++;
            }
        }

        public Material(Effect effect)
        {
            Effect = effect;
            Effect.GetSubshaders(SubShaders);
            activePass = SubShaders[0].ShaderPasses[0].EffectPass;
        }

        public void Apply()
        {
            Effect.ApplyStates();
            Effect.ApplyParameters();
            activePass.Apply(Engine.Device.ImmediateContext);
        }

        
        private readonly Dictionary<int, InputLayout> inputLayouts = new Dictionary<int, InputLayout>();

        public InputLayout GetInputLayout(InputElement[] elements)
        {
            int hash = (Technique, Pass, elements.GetHashCode()).GetHashCode();

            if (inputLayouts.TryGetValue(hash, out var layout))
                return layout;

            var desc = activePass.VertexShaderDescription.Variable.GetShaderDescription(0);
            var signature = desc.Signature;
            
            layout = new InputLayout(Engine.Device, signature, elements);

            if (layout != null)
                inputLayouts.Add(hash, layout);

            return layout;
        }

        public IEnumerable<KeyValuePair<string, EffectParameter>> GetParams() => Effect.GetParams();
        public void SetParameter<T>(string name, T value) => Effect.SetParameter(name, value);
        public bool SetValue<T>(string name, T value) => Effect.SetValue(name, value);

        private readonly int _instanceId = Interlocked.Increment(ref _instanceCount);

        private static volatile int _instanceCount;

        public int GetInstanceId() => _instanceId;
    }

    public class MaterialBlock
    {
        private readonly Dictionary<string, object> Parameters = new Dictionary<string, object>();

        public T GetValue<T>(string name)
        {
            if (Parameters.TryGetValue(name, out var result))
                return (T)result;

            return default;
        }

        public void SetParameter<T>(string name, T value)
        {
            Parameters[name] = value;
        }

        internal void Apply(Material material)
        {
            Profiler.Start("Block Apply");

            foreach (var pair in Parameters)
                material.SetParameter(pair.Key, pair.Value);

            Profiler.Stop();
        }
    }
}