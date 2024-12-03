using System.Collections.Generic;
using System.ComponentModel;
using SharpDX;
using SharpDX.Direct3D11;

namespace Spark
{
    [ExecuteInEditor]
    public sealed class Camera : Component, IUpdate, IDrawDebug
    {
        public static Transform Main { get; internal set; }
        public static Camera MainCamera { get; internal set; }

        public float NearPlane = .1f;
        public float FarPlane = 2048;
        public float FieldOfView = 45;
        public RenderView Target;
        public int Depth;

        [Browsable(false)]
        public bool IsEditor;

        public Matrix View { get; private set; }
        public Matrix Projection { get; private set; }
        public float AspectRatio { get; private set; }

        public Camera()
        {
            Target = RenderView.First;
        }

        internal void MakeMain()
        {
            Main = Transform;
            MainCamera = this;
        }

        public void Update()
        {
            var translation = Transform.WorldPosition;
            var forward = Transform.Forward;
            var up = Transform.Up;

            //if (AspectRatio == 0)
            //{
                Vector2 viewport = Target.Size;
                AspectRatio = viewport.X / viewport.Y;
            //}

            var vFOV = MathHelper.DegreesToRadians(FieldOfView);
            View = Matrix.LookAtLH(translation, translation + forward, up);
            Projection = Matrix.PerspectiveFovLH(vFOV, AspectRatio, NearPlane, FarPlane);
        }

        public static Matrix ViewMatrix(Vector3 translation, Quaternion rotation)
        {
            var forward = Vector3.Transform(Vector3.ForwardLH, rotation);
            var up = Vector3.Transform(Vector3.Up, rotation);
            var view = Matrix.LookAtLH(translation, translation + forward, up);

            return view;
        }

        public static Matrix ProjectionMatrix(Vector2 viewPortSize, float fov, float near, float far)
        {
            var aspect = viewPortSize.X / viewPortSize.Y;
            var vFOV = MathHelper.DegreesToRadians(fov);
            var projection = Matrix.PerspectiveFovLH(vFOV, aspect, near, far);

            return projection;
        }

        public static Matrix ViewProjectionMatrix(Vector2 viewPortSize, Vector3 translation, Quaternion rotation, float fov, float near, float far)
        {
            var view = ViewMatrix(translation, rotation);
            var projection = ProjectionMatrix(viewPortSize, fov, near, far);
            return view * projection;
        }

        public Matrix GetViewProjectionMatrix(float near, float far)
        {
            var translation = Transform.WorldPosition;
            var forward = Transform.Forward;
            var up = Transform.Up;
            var aspect = AspectRatio;

            if (aspect == 0)
            {
                Vector2 viewport = Target.Size;
                aspect = viewport.X / viewport.Y;
            }

            var view = Matrix.LookAtLH(translation, translation + forward, up);
            var vFOV = MathHelper.DegreesToRadians(FieldOfView);
            var projection = Matrix.PerspectiveFovLH(vFOV, aspect, near, far);

            return view * projection;
        }

        public Matrix GetProjectionMatrix(float near, float far)
        {
            var aspect = AspectRatio;

            if (aspect == 0)
            {
                Vector2 viewport = Target.Size;
                aspect = viewport.X / viewport.Y;
            }

            var vFOV = MathHelper.DegreesToRadians(FieldOfView);
            var projection = Matrix.PerspectiveFovLH(vFOV, aspect, near, far);

            return projection;
        }

        public void Render()
        {
            Profiler.Start("Camera.Render");

            Main = Transform;
            MainCamera = this;

            var backbuffer = Target.BackBufferTarget;
            var size = Target.Size;

            Graphics.SetScissorRectangle(0, 0, (int)size.X, (int)size.Y);
            Graphics.SetTargets(backbuffer);
            Graphics.ClearRenderTargetView(backbuffer, new Color4(0, 0, 0, 1));

            Update();
            SetShaderParams();

            Target.Pipeline?.Clear(this);

            CullAndFillCommandBuffer();

            Target.Pipeline?.Render(this);

            Profiler.Stop();
        }

        public void DrawDebug()
        {
            if(IsEditor) return;

            Vector4 color = new Vector4(0, 1f, .4f, 1f);
            Graphics.Lines.DrawFrustum(Matrix.Invert(View * Projection), color);
        }

        public void SetShaderParams()
        {
            Profiler.Start("Camera.SetShaderParams");

            var view = View;
            var viewInv = Matrix.Invert(View);
            var proj = Projection;
            var projInv = Matrix.Invert(Projection);
            var camerapos = Transform.Matrix.TranslationVector;
            float logz = (float)(2f / System.Math.Log(1 + FarPlane, 2) * 0.5f);
            var time = Time.TotalTime;

            foreach (var pair in Effect.MasterEffects)
            {
                pair.Value.SetParameter("CameraPosition", camerapos);
                pair.Value.SetParameter("View", view);
                pair.Value.SetParameter("ViewInverse", viewInv);
                pair.Value.SetParameter("Projection", proj);
                pair.Value.SetParameter("ProjectionInverse", projInv);
                pair.Value.SetParameter("ViewProjection", view * proj);
                pair.Value.SetParameter("Time", time);
                pair.Value.SetParameter("LogZ", logz);
            }

            Profiler.Stop();

            var size = Target.Size;
            Graphics.SetViewport(new ViewportF(0, 0, size.X, size.Y, 0.0f, 1.0f));
        }

        private IcoQueryResult queryResult = IcoQueryResult.Create();

        private void CullAndFillCommandBuffer()
        {
            CommandBuffer.ClearStatistics();
            CommandBuffer.Clear();

            var view = GetViewProjectionMatrix(.1f, 2048);
            var frustum = new BoundingFrustum(view);

            Profiler.Start("Frustum Culling");
            Physics.Query(view, ref queryResult);
            Profiler.Stop();

            Profiler.Start("Submit Drawcalls");
            queryResult.Draw(frustum, e => true, true);
            Profiler.Stop();
        }

        public Vector3 GetScreenPosition(Vector3 vector)
        {
            return Vector3.Project(vector, 0, 0, Target.Size.X, Target.Size.Y, 0, 1, View * Projection);
        }

        public Ray MouseRay()
        {
            var wvp = View * Projection;
            var near = Vector3.Unproject(new Vector3(Input.MousePoint.X - Target.Offset.X, Input.MousePoint.Y - Target.Offset.Y, 0), 0, 0, Target.Size.X, Target.Size.Y, NearPlane, FarPlane, wvp);
            var far = Vector3.Unproject(new Vector3(Input.MousePoint.X - Target.Offset.X, Input.MousePoint.Y - Target.Offset.Y, FarPlane), 0, 0, Target.Size.X, Target.Size.Y, NearPlane, FarPlane, wvp);

            return new Ray
            {
                Position = near,
                Direction = Vector3.Normalize(far - near)
            };
        }
    }
}