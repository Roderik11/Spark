using System.Diagnostics;
using System.IO;
using System.Xml.Schema;
using Assimp;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Spark
{
    [AssetReader(".dds", ".tga", ".jpg", ".bmp", ".png")]
    public class TextureReader : AssetReader<Texture>
    {
        public TextureCompression Compression;
        public TextureSize MaximumSize;

        public bool GenerateMips = true;
        public bool PowerOfTwo;
        public bool FlipVertical;
        public bool FlipHorizontal;
        public bool PremultiplyAlpha;
        public bool sRGB;

        public override Texture Import(string filename)
        {
            if (!File.Exists(filename))
                return default;

            Texture2D resource = Texture.FromFile(Engine.Device, filename);

            return new Texture
            {
                Resource = resource,
                View = new ShaderResourceView(Engine.Device, resource)
            };
        }
    }


    [AssetReader(".stex")]
    public class TexturePackReader : AssetReader<Texture>
    {
        public override Texture Import(string filename)
        {
            FileStream filestream = File.OpenRead(filename);
            BinaryReader reader = new BinaryReader(filestream);
            var packer = new TexturePacker();
            var texture = packer.Unpack(reader);
            filestream.Close();
            return texture;
        }
    }

    public enum TextureSize
    {
        Unlimited,
        _8192,
        _4096,
        _2048,
        _1024,
        _512,
        _256
    }

    public enum TextureCompression
    {
        None,
        Low,
        Medium,
        High,
        Extreme
    }
}
