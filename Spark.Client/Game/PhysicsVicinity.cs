using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Client
{
    public class PhysicsVicinity : Component, IUpdate
    {
        public float Range;

        public void Update()
        {
            var sphere = new BoundingSphere(Transform.WorldPosition, Range);
            var box = BoundingBox.FromSphere(sphere);

            Physics.Query(box, (e) =>
            {
                if (e.Transform != Transform)
                {
                    var ph = e.Transform.Entity.GetComponent<RigidBody>();
                    ph?.Insert();
                }
            });
        }
    }
}
