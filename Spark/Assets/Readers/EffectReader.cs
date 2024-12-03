using System.IO;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Spark
{
    [AssetReader(".fx")]
    public class EffectReader : AssetReader<Effect>
    {
        public override Effect Import(string filename)
        {
            return new Effect(filename);
        }
    }
}