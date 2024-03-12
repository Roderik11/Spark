using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ED8000
{
    using System;

    using SharpDX.Direct3D9;

    public static class DXSharing
    {
        #region ToD3D9Format()

        /// <summary>
        /// Konvertiert ein DXGI-Farbformat in ein DirectX 9 - Farbformat.
        /// </summary>
        public static Format ToD3D9(this SharpDX.DXGI.Format dxgiformat)
        {
            switch (dxgiformat)
            {
                case SharpDX.DXGI.Format.R10G10B10A2_UNorm:
                    return Format.A2B10G10R10;
                case SharpDX.DXGI.Format.B8G8R8A8_UNorm:
                    return Format.A8R8G8B8;
                case SharpDX.DXGI.Format.R16G16B16A16_Float:
                    return Format.A16B16G16R16F;

                case SharpDX.DXGI.Format.R32G32B32A32_Float:
                    return Format.A32B32G32R32F;

                case SharpDX.DXGI.Format.R16G16B16A16_UNorm:
                    return Format.A16B16G16R16;
                case SharpDX.DXGI.Format.R32G32_Float:
                    return Format.G32R32F;

                case SharpDX.DXGI.Format.R8G8B8A8_UNorm:
                    return Format.A8R8G8B8;

                case SharpDX.DXGI.Format.R16G16_UNorm:
                    return Format.G16R16;

                case SharpDX.DXGI.Format.R16G16_Float:
                    return Format.G16R16F;
                case SharpDX.DXGI.Format.R32_Float:
                    return Format.R32F;

                case SharpDX.DXGI.Format.R16_Float:
                    return Format.R16F;

                case SharpDX.DXGI.Format.A8_UNorm:
                    return Format.A8;
                case SharpDX.DXGI.Format.R8_UNorm:
                    return Format.L8;

                default:
                    return Format.Unknown;
            }
        }

        #endregion

        #region GetD3D9(Direct3D11.Texture2D)

        /// <summary>
        /// Konvertiert eine DirectX 11 Textur in eine DirectX 9 Textur.
        /// </summary>
        public static Texture GetSharedD3D9(this DeviceEx device, SharpDX.Direct3D11.Texture2D renderTarget)
        {
            if (renderTarget == null)
                return null;

            if ((renderTarget.Description.OptionFlags & SharpDX.Direct3D11.ResourceOptionFlags.Shared) == 0)
                throw new ArgumentException("Texture must be created with ResourceOptionFlags.Shared");

            Format format = ToD3D9(renderTarget.Description.Format);
            if (format == Format.Unknown)
                throw new ArgumentException("Texture format is not compatible with OpenSharedResource");

            using (var resource = renderTarget.QueryInterface<SharpDX.DXGI.Resource>())
            {
                IntPtr handle = resource.SharedHandle;
                if (handle == IntPtr.Zero)
                    throw new ArgumentNullException("Handle");
                return new Texture(device, renderTarget.Description.Width, renderTarget.Description.Height, 1, Usage.RenderTarget, format, Pool.Default, ref handle);
            }
        }

        #endregion
    }

}
