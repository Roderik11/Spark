using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark
{
    public struct RaycastResult
    {
        public Entity entity;
        public Vector3 hitPoint;
        public int meshPart;
    }

    public interface IMousePick
    {
        bool Intersects(Ray ray, out RaycastResult result);
    }


}
