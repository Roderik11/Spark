using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using SharpDX.Direct3D9;
using Spark;

namespace ED8000
{
    using System;
    using System.Windows;
    using System.Windows.Interop;

    using SharpDX.Direct3D9;

    public class DXImageSource : D3DImage, IDisposable
    {
        public DXImageSource()
        {
            StartD3D9();
        }
        ~DXImageSource() { this.Dispose(false); }

        public void Dispose() { this.Dispose(true); }

        protected void Dispose(bool disposing)
        {
            if (this.IsDisposed)
                return;

            if (disposing)
            {
                //Nötig damit D3DImage intern Resourcen freigibt und weiß, dass nichts mehr kommt.
                this.SetBackBuffer((Texture)null);
                GC.SuppressFinalize(this);
            }

            //Gibt das zugeordnete DirectX 9 Device frei
            EndD3D9();

            this._isDisposed = true;
        }

        bool _isDisposed;

        public bool IsDisposed { get { return this._isDisposed; } }

        public void Invalidate()
        {
            if (this.IsDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            if (this._backBuffer != null)
            {
                //Sperrt das D3DImage und erneuert es.
                this.Lock();
                this.AddDirtyRect(new Int32Rect(0, 0, base.PixelWidth, base.PixelHeight));
                this.Unlock();
            }
        }

        private SharpDX.Direct3D11.Texture2D dx11texture;

        public void SetBackBuffer(SharpDX.Direct3D11.Texture2D texture)
        {
            if (texture != null && dx11texture != null)
            {
                if (texture.NativePointer == dx11texture.NativePointer) return;
            }

            dx11texture = texture;

            SetBackBuffer(d3d9.DXDevice.GetSharedD3D9(texture));
        }

        Texture _backBuffer;

        /// <summary>
        /// Setzt eine aktuellere Backbuffer-Textur und erneuert das zugrunde liegende D3DImage.
        /// </summary>
        /// <param name="texture"></param>
        public void SetBackBuffer(Texture texture)
        {
            if (this.IsDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            Texture toDelete = null;
            try
            {
                if (texture != this._backBuffer)
                {
                //    // if it's from the private (SDX9ImageSource) D3D9 device, dispose of it
                //    if (this._backBuffer != null && this._backBuffer.Device.NativePointer == d3d9.DXDevice.NativePointer)
                //        toDelete = this._backBuffer;
                    this._backBuffer = texture;
                }

                if (texture != null)
                {
                    using (Surface surface = texture.GetSurfaceLevel(0))
                    {
                        this.Lock();
                        this.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
                        this.AddDirtyRect(new Int32Rect(0, 0, base.PixelWidth, base.PixelHeight));
                        this.Unlock();
                    }
                }
            }
            finally
            {
                if (toDelete != null)
                {
                    toDelete.Dispose();
                }
            }
        }

        #region (private, static / shared) D3D9: d3d9

        static int activeClients;
        static D3D9 d3d9;

        /// <summary>
        /// Erstellt und registriert ein neues DirectX 9 Device.
        /// </summary>
        private static void StartD3D9()
        {
            if (activeClients == 0)
                d3d9 = new D3D9(1, 1);
            activeClients++;
        }

        /// <summary>
        /// Disposed ein bestehendes DirectX 9 Device.
        /// </summary>
        private static void EndD3D9()
        {
            activeClients--;
            if (activeClients == 0)
                d3d9.Dispose();
        }

        #endregion
    }
}
