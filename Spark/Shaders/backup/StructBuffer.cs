using System.Collections.Generic;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Spark
{
    public class StructBuffer<T> where T: struct
    {
        private DataStream _dataStream;
        private DataBox _dataBox;
        private Buffer _buffer;
        private bool IsDirty;
        private T[] array;

        public Buffer Buffer { get { return _buffer; } }

        public StructBuffer(int elements)
        {
            array = new T[elements];

            int stride = Utilities.SizeOf<T>();
            int sizeInBytes = stride * elements;

            _buffer = new Buffer(Engine.Device, new BufferDescription
            {
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.VertexBuffer,
                SizeInBytes = sizeInBytes,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0
            });

            _dataStream = new DataStream(sizeInBytes, true, true);
            _dataBox = new DataBox(_dataStream.DataPointer, 0, sizeInBytes);
        }

        public void Commit()
        {
            if (!IsDirty) return;

            _dataStream.Position = 0;
            _dataStream.WriteRange(array);
            _dataStream.Position = 0;

            Engine.Device.ImmediateContext.UpdateSubresource(_dataBox, _buffer, 0);

            IsDirty = false;
        }

        public bool SetElement(int index, T value)
        {
            if (index < array.Length)
                array[index] = value;

            IsDirty = true;

            return true;
        }
    }
}