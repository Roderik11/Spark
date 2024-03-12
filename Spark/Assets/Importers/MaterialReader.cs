using System.IO;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Spark
{
    [AssetImporter(".mat")]
    public class MaterialReader : AssetReader<Material>
    {
        public override Material Import(string filename)
        {
            var text = File.ReadAllText(filename);
            var json = new JSON(text);
            var material = JSONSerializer.Deserialize<Material>(json);
            return material;
        }
    }
}