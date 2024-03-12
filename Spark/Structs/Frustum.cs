using SharpDX;

namespace Spark
{
    public struct Frustum
    {
        private Plane[] planes;

        public Frustum(Matrix value)
        {
            planes = new Plane[6];

            planes[2].Normal.X = -value.M14 - value.M11;
            planes[2].Normal.Y = -value.M24 - value.M21;
            planes[2].Normal.Z = -value.M34 - value.M31;
            planes[2].D = -value.M44 - value.M41;

            planes[3].Normal.X = -value.M14 + value.M11;
            planes[3].Normal.Y = -value.M24 + value.M21;
            planes[3].Normal.Z = -value.M34 + value.M31;
            planes[3].D = -value.M44 + value.M41;

            planes[4].Normal.X = -value.M14 + value.M12;
            planes[4].Normal.Y = -value.M24 + value.M22;
            planes[4].Normal.Z = -value.M34 + value.M32;
            planes[4].D = -value.M44 + value.M42;

            planes[5].Normal.X = -value.M14 - value.M12;
            planes[5].Normal.Y = -value.M24 - value.M22;
            planes[5].Normal.Z = -value.M34 - value.M32;
            planes[5].D = -value.M44 - value.M42;

            planes[0].Normal.X = -value.M13;
            planes[0].Normal.Y = -value.M23;
            planes[0].Normal.Z = -value.M33;
            planes[0].D = -value.M43;

            planes[1].Normal.X = -value.M14 + value.M13;
            planes[1].Normal.Y = -value.M24 + value.M23;
            planes[1].Normal.Z = -value.M34 + value.M33;
            planes[1].D = -value.M44 + value.M43;

            for (int i = 0; i < 6; i++)
            {
                float len = planes[i].Normal.Length();
                planes[i].Normal = planes[i].Normal / len;
                planes[i].D /= len;
            }
        }

        public ContainmentType Intersects(BoundingBox box)
        {
            bool flag = false;

            foreach (Plane plane in this.planes)
            {
                switch (plane.Intersects(ref box))
                {
                    case PlaneIntersectionType.Front:
                        return ContainmentType.Disjoint;

                    case PlaneIntersectionType.Intersecting:
                        flag = true;
                        break;
                }
            }

            if (!flag)
                return ContainmentType.Contains;

            return ContainmentType.Intersects;
        }

        public ContainmentType Intersects(BoundingSphere sphere)
        {
            Vector3 center = sphere.Center;
            float radius = sphere.Radius;
            int count = 0;

            foreach (Plane plane in this.planes)
            {
                float d = (plane.Normal.X * center.X + plane.Normal.Y * center.Y + plane.Normal.Z * center.Z) + plane.D;

                if (d > radius)
                    return ContainmentType.Disjoint;

                if (d < -radius)
                    count++;
            }

            if (count != 6)
                return ContainmentType.Intersects;

            return ContainmentType.Contains;
        }
    }
}