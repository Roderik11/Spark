using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.IO;

namespace Spark
{
    public class ComputeShader2
    {
        private readonly SharpDX.Direct3D11.ComputeShader shader;
        private readonly List<ConstantBufferBox> constantBuffers = new List<ConstantBufferBox>();

        private readonly Dictionary<string, ResourceViewParam> resourceViewParams = new Dictionary<string, ResourceViewParam>();
        private readonly Dictionary<string, SamplerParam> samplerParams = new Dictionary<string, SamplerParam>();
        private readonly Dictionary<string, UAVParam> uavParams = new Dictionary<string, UAVParam>();

        private class ResourceViewParam
        {
            public int slot;
            public ShaderResourceView value;
        }

        private class SamplerParam
        {
            public int slot;
            public SamplerState value;
        }

        private class UAVParam
        {
            public int slot;
            public UnorderedAccessView value;
        }

        public ComputeShader2(string filename)
        {
            string name = System.IO.Path.GetFileName(filename);
            string shortname = System.IO.Path.GetFileNameWithoutExtension(name);

            object resource = Spark.Properties.Resources.ResourceManager.GetObject(shortname);
            ShaderBytecode bytecode;
            string errors = string.Empty;

            if (resource != null)
            {
                byte[] data = (byte[])resource;
                bytecode = ShaderBytecode.Compile(data, "cs_5_0", ShaderFlags.None, EffectFlags.None, "");
            }
            else
            {
                filename = Path.Combine(Engine.ResourceDirectory, filename);
                string file = System.IO.File.ReadAllText(filename);
                bytecode = ShaderBytecode.Compile(file, "Main", "cs_5_0", ShaderFlags.None, EffectFlags.None, "");
            }

            shader = new SharpDX.Direct3D11.ComputeShader(Engine.Device, bytecode);
            var reflection = new ShaderReflection(bytecode);

            for (int i = 0; i < reflection.Description.ConstantBuffers; i++)
            {
                var cbuffer = new ConstantBufferBox(reflection.GetConstantBuffer(i));
                constantBuffers.Add(cbuffer);
            }

            int count = reflection.Description.BoundResources;
            for (int i = 0; i < count; i++)
            {
                var resourceBindDesc = reflection.GetResourceBindingDescription(i);

                if(resourceBindDesc.Type == ShaderInputType.Texture)
                    resourceViewParams.Add(resourceBindDesc.Name, new ResourceViewParam { slot = resourceBindDesc.BindPoint });

                if (resourceBindDesc.Type == ShaderInputType.Sampler)
                    samplerParams.Add(resourceBindDesc.Name, new SamplerParam { slot = resourceBindDesc.BindPoint });

                if (resourceBindDesc.Type == ShaderInputType.UnorderedAccessViewRWTyped)
                    uavParams.Add(resourceBindDesc.Name, new UAVParam { slot = resourceBindDesc.BindPoint });
            }
        }

        public void SetParameter(string name, SamplerState sampler)
        {
            if(samplerParams.TryGetValue(name, out var param))
                param.value =sampler;
        }

        public void SetParameter(string name, ShaderResourceView resourceView)
        {
            if(resourceViewParams.TryGetValue(name,out var param))
                param.value = resourceView;
        }

        public void SetParameter(string name, UnorderedAccessView uav)
        {
            if(uavParams.TryGetValue(name, out var param))
                param.value = uav;
        }

        public void SetParameter<T>(string name, T value) where T : struct
        {
            if (shader == null) return;

            foreach(var buffer in constantBuffers)
                buffer.SetParameter(name, value);
        }

        public void Dispatch(int x, int y, int z)
        {
            Engine.Device.ImmediateContext.ComputeShader.Set(shader);

            for (int i = 0; i < constantBuffers.Count; i++)
            {
                constantBuffers[i].Commit();
                Engine.Device.ImmediateContext.ComputeShader.SetConstantBuffer(i, constantBuffers[i].Buffer);
            }

            foreach (var pair in resourceViewParams)
                Engine.Device.ImmediateContext.ComputeShader.SetShaderResource(pair.Value.slot, pair.Value.value);

            foreach (var pair in samplerParams)
                Engine.Device.ImmediateContext.ComputeShader.SetSampler(pair.Value.slot, pair.Value.value);

            foreach (var pair in uavParams)
                Engine.Device.ImmediateContext.ComputeShader.SetUnorderedAccessView(pair.Value.slot, pair.Value.value);

            Engine.Device.ImmediateContext.Dispatch(x, y, z);


            Engine.Device.ImmediateContext.ComputeShader.Set(null);

            foreach (var pair in resourceViewParams)
                Engine.Device.ImmediateContext.ComputeShader.SetShaderResource(pair.Value.slot, null);

            foreach (var pair in uavParams)
                Engine.Device.ImmediateContext.ComputeShader.SetUnorderedAccessView(pair.Value.slot, null);
        }
    }
}
