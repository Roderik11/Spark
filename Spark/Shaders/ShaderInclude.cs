using System;
using System.IO;
using SharpDX.D3DCompiler;

namespace Spark
{
    public class ShaderInclude : Include
    {
        #region Include Members

        public void Close(System.IO.Stream stream)
        {
            // dunno
        }

        public Stream Open(IncludeType type, string fileName, Stream parentStream)
        {
            object vs = Properties.Resources.ResourceManager.GetObject(System.IO.Path.GetFileNameWithoutExtension(fileName));

            if (vs != null)
                return new MemoryStream((byte[])vs);

            return null;
        }

        #endregion Include Members

        #region ICallbackable Members

        public IDisposable Shadow
        {
            get;
            set;
        }

        #endregion ICallbackable Members

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion IDisposable Members
    }
}