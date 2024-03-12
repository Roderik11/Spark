using SharpDX;

namespace Spark
{
    public struct OrientedBoundingBox
    {
        public BoundingBox AABB;
        public Matrix Transform;

        public OrientedBoundingBox(BoundingBox aabb, Matrix transform)
        {
            AABB = aabb;
            Transform = transform;
        }

        public Vector3[] GetCorners()
        {
            Vector3[] points = AABB.GetCorners();
            Vector3.TransformCoordinate(points, ref Transform, points);
            return points;
        }
    }
}