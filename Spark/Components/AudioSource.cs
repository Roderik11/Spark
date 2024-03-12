using SharpDX;

namespace Spark
{
    public class AudioSource : Component, ISpatial
    {
        public float Range = 10;

        public BoundingBox BoundingBox { get; protected set; }
        public BoundingSphere BoundingSphere { get; protected set; }
        public Icoseptree SpatialNode { get; set; }

        public AudioSource()
        {
        }

        public void UpdateBounds()
        {
            BoundingSphere = new BoundingSphere(Transform.WorldPosition, Range);
            BoundingBox = BoundingBox.FromSphere(BoundingSphere);
        }
    }
}