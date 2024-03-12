using SharpDX;

namespace Spark
{
    public class Spline : Component, ISpatial, IDraw
    {
        public Color3 Color;
        public Mesh Mesh;

        private Material Material;
        private MaterialBlock Params = new MaterialBlock();

        public BoundingBox BoundingBox { get; protected set; }
        public BoundingSphere BoundingSphere { get; protected set; }
        public Icoseptree SpatialNode { get; set; }

        public Spline()
        {
            Color = new Color3(1, 1, 1);
            Mesh = Mesh.CreateSphere(1, 8, 8);
            Material = new Material(new Effect("light_point"));
            Material.Effect.BlendState = States.BlendAddColorOverwriteAlpha;
            Material.Effect.DepthStencilState = States.ZReadNoZWriteNoStencil;
        }

        public void UpdateBounds()
        {
            Vector3[] points = Mesh.BoundingBox.GetCorners(Transform.Matrix);
            BoundingBox = BoundingBox.FromPoints(points);
            BoundingSphere = BoundingSphere.FromPoints(points);
        }

        public void Draw()
        {
            if (Mesh == null) return;

            Params.SetParameter("World", Transform.Matrix);
            Params.SetParameter("LightColor", Color.ToVector3());
            Mesh.Render(Material, Params);
        }
    }
}