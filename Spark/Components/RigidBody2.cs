using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jitter.Collision.Shapes;
using Jitter.LinearMath;
using SharpDX;

namespace Spark
{
    public class RigidBody2 : Component, IUpdate, IDrawDebug
    {
        private Vector3 shift;
        private bool _isStatic;
        public Jitter.Dynamics.RigidBody JBody;

        public float Mass { get; set; }

        public bool IsStatic
        {
            get
            {
                return _isStatic;
            }
            set
            {
                if (_isStatic != value)
                {
                    _isStatic = value;

                    if (JBody != null)
                        JBody.IsStatic = value;
                }
            }
        }

        public RigidBody2()
        {
        }

        protected override void Awake()
        {
            CreateJBody();
        }

        public void Update()
        {
            if (JBody == null) return;

            if (!IsStatic)
            {
                Transform.Set(JBody.Position, JQuaternion.CreateFromMatrix(JBody.Orientation));
            }
            else if (Transform.IsDirty)
            {
                JBody.Orientation = Matrix.RotationQuaternion(Quaternion.RotationMatrix(Transform.Matrix));
                JBody.Position = Transform.WorldPosition;
            }
        }

        private Shape CreateShape()
        {
            var colliders = Entity.GetComponentsInChildren<Collider>();

            if (colliders.Count == 1)
            {
                return colliders[0].Shape;
            }
            else if (colliders.Count > 1)
            {
                CompoundShape.TransformedShape[] shapes = new CompoundShape.TransformedShape[colliders.Count];

                for (int i = 0; i < colliders.Count; i++)
                {
                    Collider coll = colliders[i];
                    shapes[i] = new CompoundShape.TransformedShape(coll.Shape, JMatrix.Identity, JVector.Zero);
                }

                var result = new CompoundShape(shapes);
                shift = result.Shift;
                return result;
            }

            return null;
        }

        private void CreateJBody()
        {
            var shape = CreateShape();
            if (shape == null) return;

            JBody = new Jitter.Dynamics.RigidBody(shape);
            JBody.EnableDebugDraw = true;
            JBody.Tag = Entity;
            JBody.Position = Transform.Position;
            JBody.Orientation = Matrix.RotationQuaternion(Quaternion.RotationMatrix(Transform.Matrix));
            JBody.IsStatic = IsStatic;
            JBody.AllowDeactivation = true;

            Physics.World.AddBody(JBody);
        }

        public void DrawDebug()
        {
            if (JBody != null)
                JBody.DebugDraw(JitterDebugDraw.Instance);
        }

        public override void Destroy()
        {
            if (JBody != null)
            {
                Physics.World.RemoveBody(JBody);
                JBody = null;
            }
        }
    }

}
