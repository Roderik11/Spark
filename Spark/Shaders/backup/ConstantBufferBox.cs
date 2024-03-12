using System.Collections.Generic;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Spark
{
    public sealed class ConstantBufferBox
    {
        private DataStream _dataStream;
        private DataBox _dataBox;
        private Buffer _buffer;
        private bool IsDirty;
        private Dictionary<string, int> Parameters = new Dictionary<string, int>();

        public Buffer Buffer { get { return _buffer; } }

        public ConstantBufferBox(ConstantBuffer cbuffer)
        {
            _buffer = new Buffer(Engine.Device, new BufferDescription
            {
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ConstantBuffer,
                SizeInBytes = cbuffer.Description.Size,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0
            });
          
            _dataStream = new DataStream(cbuffer.Description.Size, true, true);
            _dataBox = new DataBox(_dataStream.DataPointer, 0, cbuffer.Description.Size);

            for (int i = 0; i < cbuffer.Description.VariableCount; i++)
            {
                ShaderReflectionVariable variable = cbuffer.GetVariable(i);
                Parameters.Add(variable.Description.Name, variable.Description.StartOffset);
            }
        }

        public void Commit()
        {
            if (!IsDirty) return;

            _dataStream.Position = 0;
            Engine.Device.ImmediateContext.UpdateSubresource(_dataBox, _buffer, 0);

            IsDirty = false;
        }

        public bool SetParameter<T>(string name, T value) where T : struct
        {
            if (!Parameters.ContainsKey(name)) return false;

            _dataStream.Position = Parameters[name];
            _dataStream.Write(value);

            IsDirty = true;

            return true;
        }
    }
}