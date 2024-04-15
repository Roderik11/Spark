using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SharpDX;

namespace Spark
{
    public class StaticMeshRenderer : Component, ISpatial, IDraw, IMousePick, IDrawDebug
    {
        public StaticMesh StaticMesh;

        protected MaterialBlock Params;

        private Vector3 worldPosition;
        private Vector3[] points = new Vector3[8];
        public BoundingBox _boundingBox;
        public BoundingSphere _boundingSphere;

        public BoundingBox BoundingBox => _boundingBox;
        public BoundingSphere BoundingSphere => _boundingSphere;
        public Icoseptree SpatialNode { get; set; }

        public StaticMeshRenderer()
        {
            Params = new MaterialBlock();
        }

        private IStaticMesh GetMesh()
        {
            var lodgroup = StaticMesh.LODGroup;
            var distance = Vector3.Distance(worldPosition, Camera.Main.WorldPosition);
            var size = DistanceAndDiameterToScreenRatio(distance, _boundingSphere.Radius * 1);
            var min = Math.Min(lodgroup.Ranges.Count, StaticMesh.LODs.Count);

            for (int i = 0; i < min; i++)
            {
                if (size > lodgroup.Ranges[i])
                {
                    return (i == 0) ? StaticMesh : StaticMesh.LODs[i - 1] as IStaticMesh;
                }
            }

            // if it has no LODs, cull it early
            if (size < .01f) return null;

            return min > 0 ? StaticMesh.LODs[min - 1] as IStaticMesh : StaticMesh;
        }

        //Get the screen size of an object in pixels, given its distance and diameter.
        float DistanceAndDiameterToScreenRatio(float distance, float diameter)
        {
            float pixelSize = (diameter * MathHelper.RadiansToDegrees(Camera.MainCamera.Target.Size.Y)) / (distance * Camera.MainCamera.FieldOfView);
            return pixelSize / Camera.MainCamera.Target.Size.Y;
        }

        public void UpdateBounds()
        {
            var mesh = StaticMesh.Mesh;

            worldPosition = Transform.WorldPosition;

            mesh.BoundingBox.GetCorners(points, mesh.RootRotation * Transform.Matrix);
            _boundingBox = BoundingBox.FromPoints(points);
            _boundingSphere = BoundingSphere.FromPoints(points);
        }

        public void DrawDebug()
        {
            var mesh = StaticMesh.Mesh;

            Vector4 color = new Vector4(0, 1f, .4f, 1f);
            // draw obb
            Graphics.Lines.Draw(mesh.BoundingBox, Transform.Matrix, color);
            // draw abb        
            //Graphics.Lines.Draw(BoundingBox, color);
        }

        public void Draw()
        {
            var lod = GetMesh();
            if (lod == null || lod.Mesh == null) return;

            Params.SetParameter("World", lod.Mesh.RootRotation * Transform.Matrix);
            foreach(var element in lod.MeshParts)
                lod.Mesh.Render(element.MeshPart, element.Material, Params);
        }

        public bool Intersects(Ray ray, out RaycastResult result)
        {
            result = new RaycastResult();
            var mesh = GetMesh()?.Mesh;
            if (mesh == null) return false;
            return mesh.Intersects(ray, Entity.Transform.Matrix, out result);
        }
    }
}