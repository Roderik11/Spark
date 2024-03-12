using System.Collections.Generic;
using SharpDX;
using System;
using SharpDX.Direct3D11;
using System.Windows.Forms;

namespace Spark
{
    public class DirectionalLight : Component, IDraw, ILight
    {
        public Color3 Color = new Color3(1, 1, 1);
        public float Intensity = 1;
        public bool CastShadows = true;

        private readonly Material Material;
        private readonly MaterialBlock Params;

        private readonly Vector2[] cascades = new Vector2[4];
        private readonly Matrix[] cascadeProjectionMatrices = new Matrix[4];
        private readonly Matrix[] lightSpace = new Matrix[4];

        private IcoQueryResult queryResult = IcoQueryResult.Create();

        private readonly Vector3[] frustumCorners =
        {
            new Vector3(-1, 1, 0),
            new Vector3( 1, 1, 0),
            new Vector3( 1,-1, 0),
            new Vector3(-1,-1, 0),
            new Vector3(-1, 1, 1),
            new Vector3( 1, 1, 1),
            new Vector3( 1,-1, 1),
            new Vector3(-1,-1, 1),
        };

        public DirectionalLight()
        {
            Material = new Material ( new Effect("light_directional") );
            Params = new MaterialBlock();
            Params.SetParameter("sampData", Samplers.ClampedPoint2D);
        }

        public void Draw()
        {
            CommandBuffer.Enqueue(RenderPass.ShadowMap, DrawShadows);

            Params.SetParameter("LightColor", Color.ToVector3());
            Params.SetParameter("LightDirection", Transform.Forward);
            Params.SetParameter("LightIntensity", Intensity);
            Params.SetParameter("Projection", Camera.MainCamera.Projection);

            Mesh.Quad.Render(Material, Params);
        }


        void DrawShadows()
        {
            CommandBuffer.Push();

            var camera = Camera.MainCamera;

            cascades[0] = new Vector2(camera.NearPlane, 16);
            cascades[1] = new Vector2(16, 64);
            cascades[2] = new Vector2(64, 256);
            cascades[3] = new Vector2(256, 1024);

            for (int i = 0; i < cascades.Length; i++)
                cascadeProjectionMatrices[i] = camera.GetProjectionMatrix(cascades[i].X, cascades[i].Y);

            var texArray = DeferredRenderer.Current.ShadowTextureArray;
            Graphics.SetViewport(new ViewportF(0, 0, texArray.Description.Width, texArray.Description.Height, 0.0f, 1.0f));

            int count = 4;

            for (int i = 0; i < count; i++)
            {
                Graphics.ClearDepthStencilView(texArray.DepthStencilViews[i], DepthStencilClearFlags.Depth, 1, 0);

                if (!CastShadows) continue;

                Graphics.SetTargets(texArray.DepthStencilViews[i]);
                DrawCascade(i);
            }

            Params.SetParameter("LightSpaces", lightSpace);
            Params.SetParameter("Cascades", cascades);

            CommandBuffer.Pop();
        }

        void DrawCascade(int index)
        {
            var camera = Camera.MainCamera;
            var cameraView = camera.View;
            var cameraProjection = camera.Projection;
            var cameraViewProjection = cameraView * cameraProjection;
            var cameraPosition = Camera.Main.Position;

            var cascadeView = Matrix.LookAtLH(Vector3.Zero, camera.Transform.Forward, camera.Transform.Up);
            var cascadeProjection = cascadeProjectionMatrices[index];
            var cascadeViewProjection = cascadeView * cascadeProjection;

            var cascadeFrustum = new BoundingFrustum(cascadeViewProjection);
            cascadeFrustum.GetCorners(frustumCorners);

            var casc = cascades[index];
            var frustumCenter = camera.Transform.Forward * (casc.X + (casc.Y - casc.X) / 2);
            float radius =  (frustumCorners[0] - frustumCorners[6]).Length() / 2;
            
            float texelsPerUnit = DeferredRenderer.Current.ShadowTextureArray.Description.Width / (radius * 2);
            var scalar = Matrix.Scaling(texelsPerUnit);
            var baseLookAt = Transform.Forward * -1;

            var lookat = scalar * Matrix.LookAtLH(Vector3.Zero, baseLookAt, Vector3.Up);
            var invLookat = Matrix.Invert(lookat);

            frustumCenter = Vector3.TransformCoordinate(frustumCenter, lookat);
            frustumCenter.X = (float)Math.Floor(frustumCenter.X);
            frustumCenter.Y = (float)Math.Floor(frustumCenter.Y);
            frustumCenter = Vector3.TransformCoordinate(frustumCenter, invLookat);

            var view = new Matrix(
                1, 0, 0, 0,
                0, 0, -1, 0,
                Transform.Forward.X, Transform.Forward.Y, Transform.Forward.Z, 0,
                frustumCenter.X, frustumCenter.Y, frustumCenter.Z, 1
            );

            var lightView = Matrix.Invert(view);
            var lightProjection = Matrix.OrthoOffCenterLH(-radius, radius, -radius, radius, -radius, radius);
            var lightViewProjection = lightView * lightProjection;

            lightSpace[index] = lightViewProjection;

            var camInv = Matrix.LookAtLH(Vector3.Zero, Camera.Main.Forward, Camera.Main.Up) * cameraProjection;

            Params.SetParameter("CameraInverse", Matrix.Invert(camInv));
            Params.SetParameter("CameraPosition", cameraPosition);

            foreach (var pair in Effect.MasterEffects)
            {
                pair.Value.SetParameter("ShadowMap", DeferredRenderer.Current.ShadowTextureArray);
                pair.Value.SetParameter("CameraPosition", cameraPosition);
                pair.Value.SetParameter("View", lightView);
                pair.Value.SetParameter("Projection", lightProjection);
            }

            bool IsValid(IComponent e)
            {
                if (e is ILight) return false;
                return (e is ISpatial);
            }

            frustumCenter += cameraPosition;

            var cullView = Matrix.Invert(new Matrix(
                1, 0, 0, 0,
                0, 0, -1, 0,
                Transform.Forward.X, Transform.Forward.Y, Transform.Forward.Z, 0,
                frustumCenter.X, frustumCenter.Y, frustumCenter.Z, 1
            ));

            var cullMatrix = cameraView * cascadeProjection;

            if(index < 2)
                cullMatrix = cullView * lightProjection;

            Profiler.Start("Query");
            Physics.Query(cullMatrix, ref queryResult);
            var cullFrustum = new BoundingFrustum(cullMatrix);
            Profiler.Stop();

            Profiler.Start("Submit");
            queryResult.Draw(cullFrustum, IsValid);
            Profiler.Stop();

            CommandBuffer.Execute(RenderPass.Shadow);
            CommandBuffer.Clear();
        }
    }
}