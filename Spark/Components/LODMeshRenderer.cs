using System;
using System.Collections.Generic;
using SharpDX;

namespace Spark
{
    public class LODMeshRenderer : Component, ISpatial, IDraw, IMousePick, IDrawDebug
    {
        protected MaterialBlock Params;

        private Vector3 worldPosition;
        private Vector3[] points = new Vector3[8];

        public List<LODMesh> LODs = new List<LODMesh>();

        public BoundingBox BoundingBox { get; protected set; }
        public BoundingSphere BoundingSphere { get; protected set; }
        public Icoseptree SpatialNode { get; set; }

        public LODMeshRenderer()
        {
            Params = new MaterialBlock();
        }

        private LODMesh GetLODMesh()
        {
            var dist = Vector3.DistanceSquared(worldPosition, Camera.Main.WorldPosition);

            for (int i = 0; i < LODs.Count; i++)
            {
                if(dist < LODs[i].RangeSqrd)
                    return LODs[i];
            }

            return null;
        }

        public void UpdateBounds()
        {
            var mesh = LODs[0].Mesh;

            worldPosition = Transform.WorldPosition;

            mesh.BoundingBox.GetCorners(points, mesh.RootRotation * Transform.Matrix);
            BoundingBox = BoundingBox.FromPoints(points);
            BoundingSphere = BoundingSphere.FromPoints(points);
        }

        public void DrawDebug()
        {
            var mesh = LODs[0].Mesh;

            Vector4 color = new Vector4(0, 1f, .4f, 1f);
            // draw obb
            Graphics.Lines.Draw(mesh.BoundingBox, Transform.Matrix, color);
            // draw abb        
            //Graphics.Lines.Draw(BoundingBox, color);
        }

        public void Draw()
        {
            var lod = GetLODMesh();
            if (lod == null) return;

            Params.SetParameter("World", lod.Mesh.RootRotation * Transform.Matrix);
            lod.Mesh.Render(lod.Materials, Params, lod.StartIndex);
        }

        public bool Intersects(Ray ray, out RaycastResult result)
        {
            result = new RaycastResult();
            var mesh = GetLODMesh()?.Mesh;
            if (mesh == null) return false;

            return mesh.Intersects(ray, Entity.Transform.Matrix, out result);
        }
    }

    [Serializable]
    public class LODMesh
    {
        public Mesh Mesh;
        public float Range;
        public int StartIndex;
        public List<Material> Materials = new List<Material>();

        public float RangeSqrd => Range * Range;
    }


}