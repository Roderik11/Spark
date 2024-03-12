using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Spark
{
    public static class States
    {
        #region blending

        public static BlendState BlendNone { get; set; }
        public static BlendState BlendAlpha { get; set; }
        public static BlendState BlendAddColorOverwriteAlpha { get; set; }
        public static BlendState BlendAlphaToCoverage { get; set; }

        public static RasterizerState FrontCull { get; set; }
        public static RasterizerState BackCull { get; set; }
        public static RasterizerState NoCull { get; set; }
        public static RasterizerState Scissor { get; set; }
        public static RasterizerState Wireframe { get; set; }

        #endregion blending

        #region depth & stencil

        public static DepthStencilState ZReadZWriteOff { get; set; }
        public static DepthStencilState ZReadZWriteGreater { get; set; }
        public static DepthStencilState ZReadZWriteNoStencil { get; set; }
        public static DepthStencilState ZReadNoZWriteNoStencil { get; set; }

        #endregion depth & stencil

        #region texture

        // public static ImageLoadInformation Texture2DImmutable { get; set; }

        #endregion texture

        static States()
        {
            #region blending

            BlendStateDescription blendDesc = new BlendStateDescription();
           
            blendDesc.RenderTarget[0] = new RenderTargetBlendDescription
            {
                IsBlendEnabled = false,
                SourceBlend = BlendOption.One,
                DestinationBlend = BlendOption.Zero,
                SourceAlphaBlend = BlendOption.One,
                DestinationAlphaBlend = BlendOption.Zero,
                AlphaBlendOperation = BlendOperation.Add,
                BlendOperation = BlendOperation.Add,
                RenderTargetWriteMask = ColorWriteMaskFlags.All
                
            };
            BlendNone = new BlendState(Engine.Device, blendDesc);
          
            blendDesc.RenderTarget[0] = new RenderTargetBlendDescription
            {
                IsBlendEnabled = true,
                SourceBlend = BlendOption.SourceAlpha,
                DestinationBlend = BlendOption.InverseSourceAlpha,
                SourceAlphaBlend = BlendOption.SourceAlpha,
                DestinationAlphaBlend = BlendOption.InverseSourceAlpha,
                AlphaBlendOperation = BlendOperation.Add,
                BlendOperation = BlendOperation.Add,
                RenderTargetWriteMask = ColorWriteMaskFlags.All
            };
            BlendAlpha = new BlendState(Engine.Device, blendDesc);

            blendDesc.RenderTarget[0] = new RenderTargetBlendDescription
            {
                IsBlendEnabled = true,
                BlendOperation = BlendOperation.Add,
                SourceBlend = BlendOption.One,
                DestinationBlend = BlendOption.One,
                AlphaBlendOperation = BlendOperation.Add,
                SourceAlphaBlend = BlendOption.One,
                DestinationAlphaBlend = BlendOption.One,
                RenderTargetWriteMask = ColorWriteMaskFlags.All
            };
            BlendAddColorOverwriteAlpha = new BlendState(Engine.Device, blendDesc);

          //  blendDesc.AlphaToCoverageEnable = true;
            blendDesc.RenderTarget[0] = new RenderTargetBlendDescription
            {
                IsBlendEnabled = false,
                SourceBlend = BlendOption.One,
                DestinationBlend = BlendOption.Zero,
                SourceAlphaBlend = BlendOption.One,
                DestinationAlphaBlend = BlendOption.Zero,
                AlphaBlendOperation = BlendOperation.Add,
                BlendOperation = BlendOperation.Add,
                RenderTargetWriteMask = ColorWriteMaskFlags.All,

            };
            BlendNone = new BlendState(Engine.Device, blendDesc);

            #endregion

            #region rasterizer

            BackCull = new RasterizerState(Engine.Device, new RasterizerStateDescription
            {
                CullMode = CullMode.Back,
                FillMode = FillMode.Solid,
                IsDepthClipEnabled = false,
                IsMultisampleEnabled = false
            });

            FrontCull = new RasterizerState(Engine.Device, new RasterizerStateDescription
            {
                CullMode = CullMode.Front,
                FillMode = FillMode.Solid,
                IsDepthClipEnabled = false,
                IsMultisampleEnabled = false,
            });

            NoCull = new RasterizerState(Engine.Device, new RasterizerStateDescription
            {
                CullMode = CullMode.None,
                FillMode = FillMode.Solid,
                IsDepthClipEnabled = false,
                IsMultisampleEnabled = false
            });

            Scissor = new RasterizerState(Engine.Device, new RasterizerStateDescription
            {
                CullMode = CullMode.Back,
                FillMode = FillMode.Solid,
                IsDepthClipEnabled = false,
                IsMultisampleEnabled = false,
                IsScissorEnabled = true
            });

            Wireframe = new RasterizerState(Engine.Device, new RasterizerStateDescription
            {
                CullMode = CullMode.Back,
                FillMode = FillMode.Wireframe,
                IsDepthClipEnabled = false,
                IsMultisampleEnabled = false,
            });

            #endregion blending

            #region depth & stencil

            DepthStencilStateDescription zDesc = new DepthStencilStateDescription();

            zDesc = new DepthStencilStateDescription
            {
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.Less,
                IsDepthEnabled = true,
                IsStencilEnabled = false,
            };
            ZReadZWriteNoStencil = new DepthStencilState(Engine.Device, zDesc);

            zDesc = new DepthStencilStateDescription
            {
                DepthWriteMask = DepthWriteMask.Zero,
                DepthComparison = Comparison.Less,
                IsDepthEnabled = true,
                IsStencilEnabled = false,
            };
            ZReadNoZWriteNoStencil = new DepthStencilState(Engine.Device, zDesc);

            zDesc = new DepthStencilStateDescription
            {
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.Greater,
                IsDepthEnabled = true,
                IsStencilEnabled = false,
            };
            ZReadZWriteGreater = new DepthStencilState(Engine.Device, zDesc);

            zDesc = new DepthStencilStateDescription
            {
                DepthWriteMask = DepthWriteMask.Zero,
                DepthComparison = Comparison.Always,
                IsDepthEnabled = false,
                IsStencilEnabled = false,
            };
            ZReadZWriteOff = new DepthStencilState(Engine.Device, zDesc);

            #endregion depth & stencil

            #region texture loading

            //Texture2DImmutable = new ImageLoadInformation
            //{
            //    Usage = ResourceUsage.Immutable,
            //    FirstMipLevel = 0,
            //    BindFlags = BindFlags.ShaderResource,
            //    CpuAccessFlags = CpuAccessFlags.None,
            //    OptionFlags = ResourceOptionFlags.None,
            //    Format = Format.R8G8B8A8_UNorm,
            //    MipLevels = 0
            //};

            #endregion texture loading
        }
    }
}