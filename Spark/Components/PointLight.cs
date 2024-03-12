using SharpDX;
using System.Collections.Generic;

namespace Spark
{
    public class PointLight : Component, ISpatial, IDraw, ILight, IDrawDebug
    {
        public Color3 Color;
        public Mesh Mesh;
        public float Intensity;
        public float Range = 10;

        private Material Material;
        private MaterialBlock Params = new MaterialBlock();

        public BoundingBox BoundingBox { get; protected set; }
        public BoundingSphere BoundingSphere { get; protected set; }
        public Icoseptree SpatialNode { get; set; }

        public PointLight()
        {
            Color = new Color3(1, 1, 1);
            Mesh = Mesh.CreateSphere(1, 8, 8);
            Material = new Material(new Effect("light_point"));
            Material.Effect.BlendState = States.BlendAddColorOverwriteAlpha;
            Material.Effect.DepthStencilState = States.ZReadNoZWriteNoStencil;
        }

        public void UpdateBounds()
        {
            BoundingSphere = new BoundingSphere(Transform.WorldPosition, Range);
            BoundingBox = BoundingBox.FromSphere(BoundingSphere);
        }

        public void Draw()
        {
            if (Mesh == null) return;

            Transform.Scale = Vector3.One * Range;

            Params.SetParameter("World", Transform.Matrix);
            Params.SetParameter("LightColor", Color.ToVector3());
            Params.SetParameter("LightIntensity", Intensity);
            Params.SetParameter("Range", Range);

            var campos = Camera.Main.WorldPosition;
            var inside = BoundingSphere.Contains(ref campos) == ContainmentType.Contains;
            Material.Effect.RasterizerState = inside ? States.FrontCull : States.BackCull;

            Mesh.Render(Material, Params);
        }

        public void DrawDebug()
        {
            Graphics.Lines.DrawCircle(Transform.Position, Transform.Forward, Transform.Right, Range, new Vector4(Color, 1));
            Graphics.Lines.DrawCircle(Transform.Position, Transform.Right, Transform.Up, Range, new Vector4(Color, 1));
            Graphics.Lines.DrawCircle(Transform.Position, Transform.Up, Transform.Forward, Range, new Vector4(Color, 1));
            Graphics.Lines.DrawCircle(Transform.Position, Camera.Main.Up, Camera.Main.Right, Range, new Vector4(Color, 1));
        }
    }

    public interface ILight { }
}