using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark
{
    public static class Graphics
    {
        public static LineBatch Lines { get; private set; }

        private static Effect blitEffect;

        private static OutputMergerStage outputMerger;
        private static RasterizerStage rasterizer;
        private static DeviceContext deviceContext;

        private static class Quad
        {
            private static int vertexStride;
            private static Buffer vertexBuffer;
            private static Buffer indexBuffer;
            private static VertexBufferBinding vertexBufferBinding;

            static Quad()
            {
                int[] indices = { 0, 1, 2, 2, 3, 0 };

                VertexColorUV[] vertices =
                {
                    new VertexColorUV{ Position = new Vector3(1, -1, 0), Uv = new Vector2(1, 1)},
                    new VertexColorUV{ Position = new Vector3(-1, -1, 0), Uv = new Vector2(0, 1)},
                    new VertexColorUV{ Position = new Vector3(-1, 1, 0), Uv = new Vector2(0, 0)},
                    new VertexColorUV{ Position = new Vector3(1, 1, 0), Uv = new Vector2(1, 0)}
                };

                vertexStride = SharpDX.Utilities.SizeOf<VertexColorUV>();
                vertexBuffer = Geometry.CreateVertexBuffer(vertices);
                indexBuffer = Geometry.CreateIndexBuffer(indices);
                vertexBufferBinding = new VertexBufferBinding(vertexBuffer, vertexStride, 0);
            }

            public static void Draw(Effect effect = null)
            {
                if (effect != null)
                {
                    effect.Apply();
                    Engine.Device.ImmediateContext.InputAssembler.InputLayout = effect.GetInputLayout(VertexColorUV.InputElements);
                }

                Engine.Device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                Engine.Device.ImmediateContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
                Engine.Device.ImmediateContext.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);
                Engine.Device.ImmediateContext.DrawIndexed(6, 0, 0);
            }
        }

        static Graphics()
        {
            blitEffect = new Effect("blit");
            Lines = new LineBatch();
            deviceContext = Engine.Device.ImmediateContext;
            outputMerger = deviceContext.OutputMerger;
            rasterizer = deviceContext.Rasterizer;
        }

        public static void DrawFullscreenQuad(Effect effect = null)
        {
            Quad.Draw(effect);
        }

        public static void SetViewport(float x, float y, float w, float h)
        {
            rasterizer.SetViewport(new ViewportF(x, y, w, h, 0.0f, 1.0f));
            rasterizer.SetScissorRectangles(new SharpDX.Rectangle((int)x, (int)y, (int)w, (int)h));
        }

        public static void Blit(Texture texture, RenderTargetView surface, Effect effect = null)
        {   
            if (effect == null)
            {
                effect = blitEffect;
                blitEffect.SetValue("sampData", Samplers.WrappedAnisotropic);
                blitEffect.SetValue("MainTexture", texture);
            }

            outputMerger.SetTargets(surface);
            Quad.Draw(effect);
        }

        public static void ResetTargets() => outputMerger.ResetTargets();

        public static Texture CreateTexture2DArray(int width, int height, List<Texture> sources)
        {
            var texture0 = sources[0];
            int mips = texture0.Description.MipLevels;

            var desc = new Texture2DDescription
            {
                Width = width,
                Height = height,
                ArraySize = sources.Count,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                CpuAccessFlags = CpuAccessFlags.None,
                MipLevels = 8,
                OptionFlags = ResourceOptionFlags.GenerateMipMaps,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            };

            var resource = new Texture2D(Engine.Device, desc);

            rasterizer.SetViewport(new ViewportF(0, 0, width, height, 0.0f, 1.0f));
            rasterizer.SetScissorRectangles(new SharpDX.Rectangle(0, 0, width, height));

            var effect = new Effect("blit");
            for (int i = 0; i < sources.Count; i++)
            {
                var splat = sources[i];
                effect.SetValue("MainTexture", splat);

                var view = new RenderTargetViewDescription();
                view.Dimension = RenderTargetViewDimension.Texture2DArray;
                view.Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm;
                view.Texture2DArray.MipSlice = 0;
                view.Texture2DArray.FirstArraySlice = i;
                view.Texture2DArray.ArraySize = 1;

                using (var rtv = new RenderTargetView(Engine.Device, resource, view))
                {
                    Blit(splat, rtv, effect);
                }
            }

            var renderTargetViewDesc = new RenderTargetViewDescription();
            renderTargetViewDesc.Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm;
            renderTargetViewDesc.Dimension = RenderTargetViewDimension.Texture2DArray;
            renderTargetViewDesc.Texture2DArray.ArraySize = sources.Count;
            renderTargetViewDesc.Texture2DArray.FirstArraySlice = 0;

            var shaderResourceViewDesc = new ShaderResourceViewDescription();
            shaderResourceViewDesc.Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm;
            shaderResourceViewDesc.Dimension = ShaderResourceViewDimension.Texture2DArray;
            shaderResourceViewDesc.Texture2DArray.ArraySize = sources.Count;
            shaderResourceViewDesc.Texture2DArray.MipLevels = -1;
            shaderResourceViewDesc.Texture2DArray.FirstArraySlice = 0;

            var textureArray = new Texture
            {
                Description = desc,
                Resource = resource,
                Target = new RenderTargetView(Engine.Device, resource, renderTargetViewDesc),
                View = new ShaderResourceView(Engine.Device, resource, shaderResourceViewDesc)
            };

            deviceContext.GenerateMips(textureArray.View);
            return textureArray;
        }


        public static void SetTargets(params RenderTargetView[] renderTargetViews) =>
            outputMerger.SetTargets(renderTargetViews);

        public static void SetTargets(RenderTargetView renderTargetView) =>
            outputMerger.SetTargets(renderTargetView);

        public static void SetTargets(DepthStencilView depthStencilView, RenderTargetView renderTargetView) =>
            outputMerger.SetTargets(depthStencilView, renderTargetView);

        public static void SetTargets(DepthStencilView depthStencilView, params RenderTargetView[] renderTargetViews) =>
            outputMerger.SetTargets(depthStencilView, renderTargetViews);

        public static void SetBlendState(BlendState blendStateRef, Color4? blendFactor = null, int sampleMask = -1) =>
            outputMerger.SetBlendState(blendStateRef, blendFactor, sampleMask);

        public static void SetDepthStencilState(DepthStencilState depthStencilStateRef, int stencilRef = 0) =>
            outputMerger.SetDepthStencilState(depthStencilStateRef, stencilRef);

        public static void ClearRenderTargetView(RenderTargetView renderTargetViewRef, Color4 colorRGBA) =>
            deviceContext.ClearRenderTargetView(renderTargetViewRef, colorRGBA);

        public static void ClearDepthStencilView(DepthStencilView depthStencilViewRef, DepthStencilClearFlags clearFlags, float depth, byte stencil) => 
            deviceContext.ClearDepthStencilView(depthStencilViewRef, clearFlags, depth, stencil);

        public static void SetViewport(ViewportF viewport) =>
            rasterizer.SetViewport(viewport);

        public static void SetScissorRectangle(int left, int top, int right, int bottom) =>
            rasterizer.SetScissorRectangle(left, top, right, bottom);

        public static void SetRasterizerState(RasterizerState state) => rasterizer.State = state;

    }
}
