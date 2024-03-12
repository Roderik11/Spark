using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Spark
{
    public sealed class RenderView : IDisposable
    {
        public IntPtr Handle { get; private set; }

        public Point Offset;

        private bool ResizePending;
        private SwapChain Swapchain;
        private RenderPipeline _pipeline;
        private SharpDX.Direct3D11.Device Device;
        private SharpDX.DXGI.Factory Factory;

        public object Tag;
        public bool Enabled { get; set; }
        public Vector2 Size { get; private set; }

        public Texture2D BackBuffer { get; private set; }
        public Texture2D DepthBuffer { get; private set; }

        public RenderTargetView BackBufferTarget { get; private set; }
        public DepthStencilView DepthBufferTarget { get; private set; }
        public Texture BackBufferTexture { get; private set; }

        public static RenderView Active { get; private set; }
        public static RenderView First{ get; private set; }

        public static List<RenderView> All = new List<RenderView>();

        public Action<RenderView> OnRender;
        public event Action OnResized;
        public static event Action OnViewsChanged;

        public RenderPipeline Pipeline
        {
            get { return _pipeline; }
            set { _pipeline = value; InitializePipeline(); }
        }

        public RenderView(IntPtr handle)
        {
            Enabled = true;
            Device = Engine.Device;
            Factory = Engine.Factory;
            Handle = handle;
 
            CreateSwapchain();
            CreateBuffers();

            if (First == null)
            {
                First = this;
                Input.SetHandle(Handle);
            }

            All.Add(this);
            OnViewsChanged?.Invoke();
        }

        public RenderView(int width, int height)
        {
            Enabled = true;
            Device = Engine.Device;
            Factory = Engine.Factory;
            Handle = IntPtr.Zero;
            Size = new Vector2(width, height);

            CreateBuffers();

            if (First == null)
            {
                First = this;
            //    Input.SetHandle(Handle);
            }

            All.Add(this);
            OnViewsChanged?.Invoke();
        }

        public void Prepare()
        {
            Active = this;

            if (ResizePending)
            {
                ResizePending = false;
                ResizeBuffers();
            }

            Device.ImmediateContext.Rasterizer.SetViewport(new ViewportF(0, 0, Size.X, Size.Y, 0.0f, 1.0f));
        }

        public void Present()
        {
            Profiler.Start("Present");

            if (Swapchain != null)
            {
                Swapchain.Present(Engine.Settings.VSync ? 1 : 0, PresentFlags.None);
                Graphics.SetTargets(BackBufferTarget);
            }
            else
                Device.ImmediateContext.Flush();

            Device.ImmediateContext.ClearState();

            Profiler.Stop();
        }

        private void CreateSwapchain()
        {
            if (Handle == IntPtr.Zero) return;

            var desc = new SwapChainDescription()
            {
                BufferCount = 2,
                Usage = Usage.RenderTargetOutput | Usage.ShaderInput,
                OutputHandle = Handle,
                IsWindowed = true,
                ModeDescription = new ModeDescription(0, 0, new Rational(60, 1), Format.B8G8R8A8_UNorm),
                SampleDescription = new SampleDescription(1, 0),
                Flags = SwapChainFlags.None,
                SwapEffect = SwapEffect.FlipDiscard
            };

            Swapchain = new SwapChain(Factory, Device, desc);
        }

        private void CreateBuffers()
        {
            if (Swapchain != null)
            {
                BackBuffer = Swapchain.GetBackBuffer<Texture2D>(0);
                Size = new Vector2(BackBuffer.Description.Width, BackBuffer.Description.Height);
            }
            else
            {
                Texture2DDescription colordesc = new Texture2DDescription
                {
                    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                    Format = Format.B8G8R8A8_UNorm,
                    Width = (int)Size.X,
                    Height = (int)Size.Y,
                    MipLevels = 1,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    OptionFlags = ResourceOptionFlags.Shared,
                    CpuAccessFlags = CpuAccessFlags.None,
                    ArraySize = 1
                };

                BackBuffer = new Texture2D(Device, colordesc);
            }

            DepthBuffer = new Texture2D(Device, new Texture2DDescription
            {
                BindFlags = BindFlags.DepthStencil,
                Format = Format.D24_UNorm_S8_UInt,
                Width = BackBuffer.Description.Width,
                Height = BackBuffer.Description.Height,
                MipLevels = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.None,
                CpuAccessFlags = CpuAccessFlags.None,
                ArraySize = 1,
            });

            BackBufferTarget = new RenderTargetView(Device, BackBuffer);
            DepthBufferTarget = new DepthStencilView(Device, DepthBuffer);

            var view = new ShaderResourceView(Device, BackBuffer);
            BackBufferTexture = new Texture
            {
                Description = BackBuffer.Description,
                Name = "Backbuffer",
                Resource = BackBuffer,
                View = view,
                Target = BackBufferTarget
            };
        }

        public void Resize()
        {
            ResizePending = true;
        }

        public void Resize(int width, int height)
        {
            Size = new Vector2(Math.Max(1, width), Math.Max(1, height));
            // CreateBuffers();
            ResizePending = true;
        }

        public void Recreate(IntPtr handle)
        {
            BackBufferTarget.Dispose(); BackBufferTarget = null;
            DepthBufferTarget.Dispose(); DepthBufferTarget = null;
            BackBuffer.Dispose(); BackBuffer = null;
            DepthBuffer.Dispose(); DepthBuffer = null;
            BackBufferTexture.Dispose(); BackBufferTexture = null;

            if (Swapchain != null)
            {
                Swapchain.Dispose();
                Swapchain = null;
            }

            Handle = handle;

            CreateSwapchain();
            CreateBuffers();
            ResizePipeline();
        }

        private void ResizeBuffers()
        {
            BackBufferTarget.Dispose(); BackBufferTarget = null;
            DepthBufferTarget.Dispose(); DepthBufferTarget = null;
            BackBuffer.Dispose(); BackBuffer = null;
            DepthBuffer.Dispose(); DepthBuffer = null;
            BackBufferTexture.Dispose(); BackBufferTexture = null;

            Swapchain?.ResizeBuffers(2, 0, 0, Format.B8G8R8A8_UNorm, 0);

            CreateBuffers();
            ResizePipeline();
        }

        private void InitializePipeline()
        {
            Pipeline?.Initialize((int)Size.X, (int)Size.Y);
        }

        private void ResizePipeline()
        {
            Pipeline?.Resize((int)Size.X, (int)Size.Y);
            OnResized?.Invoke();
        }

        public void Dispose()
        {
            Swapchain?.Dispose();
            BackBufferTexture?.Dispose();
            DepthBuffer?.Dispose();
            All.Remove(this);
        }
    }
}