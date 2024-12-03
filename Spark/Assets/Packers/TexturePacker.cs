using SharpDX;
using System.IO;
using SharpDX.Direct3D11;

namespace Spark
{
    public class TexturePacker: AssetPacker<Texture>
    {
        public override void Pack(BinaryWriter writer, Texture tex)
        {
            var device = Engine.Device;
            var context = Engine.Device.ImmediateContext;

            var ramTexture = tex.Resource;
            bool isReadable = tex.Description.CpuAccessFlags.HasFlag(CpuAccessFlags.Read);

            if (!isReadable)
            {
                var desc = tex.Description;
                desc.CpuAccessFlags = CpuAccessFlags.Read;
                desc.Usage = ResourceUsage.Staging;
                desc.BindFlags = BindFlags.None;

                ramTexture = new Texture2D(device, desc);
                context.CopyResource(tex.Resource, ramTexture);

                System.Threading.Thread.Sleep(1);
            }

            var width = tex.Description.Width;
            var height = tex.Description.Height;
            var dataBox = Engine.Device.ImmediateContext.MapSubresource(ramTexture, 0, MapMode.Read, 0, out DataStream dataStream);
            var length = (int)dataStream.Length;
            var buff = new byte[length];

            if (!tex.Path.ToLowerInvariant().EndsWith(".dds"))
            //if (dataBox.RowPitch == size * 2)
            {
                dataStream.ReadRange(buff, 0, buff.Length);
            }
            else
            {
                int offset = 0;
                var ptr = dataBox.DataPointer;

                for (int i = 0; i < height; i++)
                {
                    dataStream.ReadRange(buff, offset, dataBox.RowPitch);
                    //Utilities.Read((IntPtr)ptr, buff, offset, dataBox.RowPitch);
                    // ptr += dataBox.RowPitch;
                    offset += dataBox.RowPitch;
                }
            }

            //var rawTexureData = Utilities.ReadStream(dataStream);
            context.UnmapSubresource(ramTexture, 0);
            dataStream.Dispose();

            if (!isReadable)
                ramTexture.Dispose();

            var rawTexureData = buff;
            var rawTextureDescription = Utils.StructToBytes(tex.Description);

            writer.Write(rawTextureDescription.Length);
            writer.Write(rawTextureDescription);
            writer.Write(dataBox.RowPitch);
            writer.Write(dataBox.SlicePitch);
            writer.Write(rawTexureData.Length);
            writer.Write(rawTexureData);
        }

        public override Texture Unpack(BinaryReader reader)
        {
            var device = Engine.Device;

            int descLength = reader.ReadInt32();
            var description = Utils.BytesToStruct<Texture2DDescription>(reader.ReadBytes(descLength));

            int rowPitch = reader.ReadInt32();
            int slicePitch = reader.ReadInt32();

            int dataLength = reader.ReadInt32();
            var data = reader.ReadBytes(dataLength);

            var stream = new DataStream(dataLength, false, true);
            stream.WriteRange(data);

            var box = new DataBox(stream.DataPointer, rowPitch, slicePitch);
            var resource = new Texture2D(device, description, new[] { box });

            return new Texture
            {
                Resource = resource,
                View = new ShaderResourceView(Engine.Device, resource)
            };
        }
    }
}

