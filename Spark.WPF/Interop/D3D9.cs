using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ED8000
{
    using System;
    using System.Runtime.InteropServices;

    using SharpDX.Direct3D9;

    public class D3D9 : IDisposable
    {
        private DeviceEx _device;
        private Texture _backBuffer;
        private bool _disposed;

        public D3D9(int width, int height)
        {
            if (_device != null) return;

            PresentParameters presentparams = new PresentParameters();
            presentparams.Windowed = true;
            presentparams.SwapEffect = SwapEffect.Discard;
            presentparams.DeviceWindowHandle = GetDesktopWindow();
            presentparams.PresentationInterval = PresentInterval.Default;
            _device = new DeviceEx(
                new Direct3DEx(),
                0,
                DeviceType.Hardware,
                IntPtr.Zero,
                CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve,
                presentparams);

            this.Reset(width, height);
        }

        public DeviceEx DXDevice
        {
            get
            {
                return _device;
            }
        }

        [DllImport("user32.dll", SetLastError = false)]
        static extern IntPtr GetDesktopWindow();

        public void Reset(int w, int h)
        {
            if (w < 1)
                throw new ArgumentOutOfRangeException("w");
            if (h < 1)
                throw new ArgumentOutOfRangeException("h");

            _backBuffer = null;

            _backBuffer = new Texture(_device, w, h, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
            _device.SetRenderTarget(0, _backBuffer.GetSurfaceLevel(0));
        }

        public Texture RenderTarget
        {
            get
            {
                return _backBuffer;
            }
        }

        /// <summary>
        /// Führt anwendungsspezifische Aufgaben durch, die mit der Freigabe, der Zurückgabe oder dem Zurücksetzen von nicht verwalteten Ressourcen zusammenhängen.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (!_disposed)
            {
                _backBuffer.Dispose();
                _device.Dispose();
                _device = null;

                _disposed = true;
            }
        }
    }
}
