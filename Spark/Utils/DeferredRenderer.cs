using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Spark
{
    public class DeferredRenderer : RenderPipeline
    {
        public Texture ReflectionMap { get; set; }
        public static DeferredRenderer Current { get; private set; }

        private Effect FXClear;
        private Effect FXCompose;
        private Effect FXReflection;
        private Effect FXSSAO;
        private Effect FXWater;

        public RenderTexture2D Albedo;
        public RenderTexture2D Normals;
        public RenderTexture2D Depth;
        public RenderTexture2D Data;
        public RenderTexture2D Composite;

        private RenderTexture2D LightBuffer;
        private RenderTexture2D ReflectionBuffer;
        private RenderTexture2D SSAOBuffer;
        private Texture randomTex;

        private Texture skyBox;
        private Texture waterNormals;

        public DepthTextureArray2D ShadowTextureArray;
        
        public RenderTargetView[] GBuffer { get; set; }

        public override void Initialize(int width, int height)
        {
            FXWater = new Effect("water");
            FXClear = new Effect("cleargbuffer");
            FXCompose = new Effect("composition");
            FXReflection = new Effect("cubemap_reflection");
            FXSSAO = new Effect("ssao");

            ShadowTextureArray = new DepthTextureArray2D(2048, 2048, 4);
            CreateRenderTextures(width, height); 

            randomTex = Engine.Assets.Load<Texture>("Textures/random.png");
            skyBox = Engine.Assets.Load<Texture>("SkyboxSet1/ThickCloudsWater/cubemap.dds");
            waterNormals = Engine.Assets.Load<Texture>("water_normals.dds");

            Current = this;
        }

        void CreateRenderTextures(int width, int height)
        {
            Albedo = new RenderTexture2D(width, height, Format.R8G8B8A8_UNorm, false);
            Normals = new RenderTexture2D(width, height, Format.R16G16B16A16_SNorm, false);
            Data = new RenderTexture2D(width, height, Format.R8G8B8A8_UNorm, false);
            Depth = new RenderTexture2D(width, height, Format.R32_Float, false);
            Composite = new RenderTexture2D(width, height, Format.R8G8B8A8_UNorm, false);
            LightBuffer = new RenderTexture2D(width, height, Format.R16G16B16A16_Float, false);
            ReflectionBuffer = new RenderTexture2D(width, height, Format.R8G8B8A8_UNorm, false);
            SSAOBuffer = new RenderTexture2D(width, height, Format.R8G8B8A8_UNorm, false);

            GBuffer = new[] { Albedo.Target, Normals.Target, Depth.Target, Data.Target };
        }

        public override void Resize(int width, int height)
        {
            Disposer.SafeDispose(ref Albedo);
            Disposer.SafeDispose(ref Normals);
            Disposer.SafeDispose(ref Data);
            Disposer.SafeDispose(ref Depth);
            Disposer.SafeDispose(ref Composite);
            Disposer.SafeDispose(ref LightBuffer);
            Disposer.SafeDispose(ref ReflectionBuffer);
            Disposer.SafeDispose(ref SSAOBuffer);

            CreateRenderTextures(width, height);
        }

        public override void Clear(Camera camera)
        {
            Current = this;
            ClearGBuffer(camera);
        }

        public override void Render(Camera camera)
        {
            DrawShadowMaps();
            camera.SetShaderParams();
            FillGBuffer(camera);
            FillLightBuffer(camera);
            ComposeFinalImage(camera);
            Graphics.Blit(Composite, camera.Target.BackBufferTarget);
            Graphics.SetTargets(camera.Target.DepthBufferTarget, camera.Target.BackBufferTarget);

            if(Engine.Settings.SSAO)
                SSAO(camera);

            Graphics.SetBlendState(States.BlendAlpha);
            Graphics.SetDepthStencilState(States.ZReadZWriteNoStencil);

            DrawPass(RenderPass.Transparent);

            if(Engine.Settings.GlobalWater)
                DrawWater(camera);
           
            Graphics.Lines.Start();
            DrawPass(RenderPass.Debug);
            Graphics.Lines.End();

            Graphics.SetTargets(camera.Target.BackBufferTarget);
            DrawPass(RenderPass.PostProcess);
            DrawPass(RenderPass.Overlay);
        }

        private void DrawWater(Camera camera)
        {
            Graphics.SetBlendState(States.BlendAlpha);
            Graphics.SetDepthStencilState(States.ZReadNoZWriteNoStencil, 0);

            var camInv = Matrix.LookAtLH(Vector3.Zero, Camera.Main.Forward, Camera.Main.Up) * Camera.MainCamera.Projection;
            Graphics.SetTargets(camera.Target.BackBufferTarget);
            FXWater.Technique = 0;
            FXWater.SetParameter("Time", Time.TotalTime);
            FXWater.SetParameter("CameraPosition", camera.Transform.WorldPosition);
            FXWater.SetParameter("ViewInverse", Matrix.Invert(camera.View));
            FXWater.SetParameter("View", camera.View);
            FXWater.SetParameter("ViewDirection", camera.Transform.Forward);
            FXWater.SetParameter("CameraInverse", Matrix.Invert(camInv));
            FXWater.SetParameter("sampData", Samplers.WrappedAnisotropic);
            FXWater.SetParameter("Albedo", Composite);
            FXWater.SetParameter("Normal", Normals);
            FXWater.SetParameter("Depth", Depth);
            FXWater.SetParameter("textureSkybox", skyBox);
            FXWater.SetParameter("waterNormals", waterNormals);
            FXWater.SetParameter("WaterHeight", 63.5f);
            Graphics.DrawFullscreenQuad(FXWater);
        }

        private void ClearGBuffer(Camera camera)
        {
            Graphics.ClearDepthStencilView(camera.Target.DepthBufferTarget, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
            Graphics.SetBlendState(States.BlendNone, null, 0xfffffff);
            Graphics.SetDepthStencilState(States.ZReadZWriteNoStencil, 0);
            Graphics.SetTargets(GBuffer);
            Graphics.DrawFullscreenQuad(FXClear);
        }

        private void FillGBuffer(Camera camera)
        {
            Profiler.Start("Draw G-Buffer");
            Graphics.SetTargets(camera.Target.DepthBufferTarget, GBuffer);
            DrawPass(RenderPass.Opaque);
            Profiler.Stop();
        }

        private void FillLightBuffer(Camera camera)
        {
            Profiler.Start("Draw Light Buffer");

            //Matrix matViewT = camera.View;
            //Matrix matViewIT = Matrix.Invert(camera.View);
            //Matrix matProjectionIT = Matrix.Invert(camera.Projection);

            //Engine.Device.ImmediateContext.OutputMerger.SetBlendState(States.BlendNone, null, 0xfffffff);
            //Engine.Device.ImmediateContext.OutputMerger.SetDepthStencilState(States.ZReadZWriteNoStencil, 0);
            //Engine.Device.ImmediateContext.Rasterizer.State = States.BackCull;

            //// set reflection target
            //Engine.Device.ImmediateContext.OutputMerger.SetTargets(ReflectionBuffer.Target);
            //Engine.Device.ImmediateContext.ClearRenderTargetView(ReflectionBuffer.Target, new Color4(0, 0, 0, 0));
            //FXReflection.SetParameter("sampReflection", Samplers.ClampedTrilinear);
            //FXReflection.SetParameter("sampData", Samplers.ClampedPoint2D);
            //FXReflection.SetParameter("Textures", new List<Texture> { GBuffer.Normals, GBuffer.Depth, GBuffer.Data });
            //FXReflection.SetParameter("texReflection", ReflectionMap);
            //FXReflection.SetParameter("CameraPosition", camera.View.GetTranslation());
            //FXReflection.SetParameter("InverseViewProjection", Matrix.Invert(camera.View * camera.Projection));
            //FXReflection.SetParameter("matProjectionInverse", matProjectionIT);
            //FXReflection.SetParameter("matViewInverse", matViewIT);
            //FullscreenQuad.Draw(FXReflection);

            foreach (var pair in Effect.MasterEffects)
            {
                pair.Value.SetParameter("Fog", Convert.ToInt32(Engine.Settings.Fog));
                pair.Value.SetParameter("Textures", new List<Texture> { Normals, Depth, Data});
                pair.Value.SetParameter("CameraInverse", Matrix.Invert(camera.View * camera.Projection));
            }

            // set light target
            Graphics.SetBlendState(States.BlendAddColorOverwriteAlpha, null, 0xfffffff);
            Graphics.SetDepthStencilState(States.ZReadNoZWriteNoStencil, 0);
            Graphics.ClearRenderTargetView(LightBuffer.Target, new Color4(0, 0, 0, 0));
            Graphics.SetTargets(LightBuffer.Target);
            DrawPass(RenderPass.Light);
            Profiler.Stop();
        }

        private void SSAO(Camera camera)
        {
            Graphics.SetBlendState(States.BlendNone, null, 0xfffffff);
            Graphics.SetDepthStencilState(States.ZReadZWriteNoStencil, 0);

            float logz = (float)(2f / Math.Log(1 + Camera.MainCamera.FarPlane, 2) * 0.5f);

            Graphics.SetTargets(SSAOBuffer.Target);
            FXSSAO.Technique = 0;
            FXSSAO.SetParameter("InvLogZ", 1f / logz);
            FXSSAO.SetParameter("sampPoint", Samplers.WrappedPoint2D);
            FXSSAO.SetParameter("sampBilinear", Samplers.WrappedBilinear2D);
            FXSSAO.SetParameter("Textures", new List<Texture> { Depth, Normals, randomTex, camera.Target.BackBufferTexture });
            Graphics.DrawFullscreenQuad(FXSSAO);

            Graphics.SetBlendState(States.BlendNone, null, 0xfffffff);

            Graphics.SetTargets(camera.Target.BackBufferTarget);
            FXSSAO.Technique = 1;
            FXSSAO.SetParameter("Textures", new List<Texture> { SSAOBuffer, SSAOBuffer, SSAOBuffer, SSAOBuffer });
            Graphics.DrawFullscreenQuad(FXSSAO);
        }

        private void DrawShadowMaps()
        {
            Profiler.Start("Draw Shadows");
            DrawPass(RenderPass.ShadowMap);
            Profiler.Stop();
        }

        private void ComposeFinalImage(Camera camera)
        {
            var camInv = Matrix.LookAtLH(Vector3.Zero, Camera.Main.Forward, Camera.Main.Up) * camera.Projection;

            Profiler.Start("ComposeFinalImage");
            Graphics.ClearRenderTargetView(Composite.Target, new Color4(0, 0, 0, 0));
            Graphics.SetTargets(Composite.Target);
            FXCompose.SetParameter("sampData", Samplers.ClampedPoint2D);
            FXCompose.SetParameter("Textures", new List<Texture> { Albedo, Data, LightBuffer, Depth });
            FXCompose.SetParameter("texReflection", ReflectionBuffer);
            FXCompose.SetParameter("CameraPosition", camera.View.TranslationVector);
            FXCompose.SetParameter("Fog", Convert.ToInt32(Engine.Settings.Fog));
            FXCompose.SetParameter("InverseViewProjection", Matrix.Invert(camInv));
            FXCompose.SetParameter("matProjectionInverse", Matrix.Invert(camera.Projection));
            FXCompose.SetParameter("matViewInverse", Matrix.Invert(camera.View));
            Graphics.DrawFullscreenQuad(FXCompose);
            Profiler.Stop();
        }
    }
}