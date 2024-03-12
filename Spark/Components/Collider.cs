using System;
using System.Collections.Generic;
using Jitter.Collision.Shapes;
using Jitter.LinearMath;
using SharpDX;
using Spark.Noise.Operator;

namespace Spark
{
    public abstract class Collider : Component
    {
        private bool isDirty;
        protected Shape _shape;

        public bool IsTrigger;

        public Shape Shape
        {
            get
            {
                if (_shape == null || isDirty)
                {
                    SetDirty(false);
                    _shape = CreateShape();
                }

                return _shape;
            }
        }

        protected abstract Shape CreateShape();

        protected void SetDirty(bool dirty = true) { isDirty = dirty; }

    }

    public sealed class BoxCollider : Collider
    {
        private Vector3 _center;
        private Vector3 _size;
       
        public Vector3 Center
        {
            get { return _center; }
            set { _center = value; }
        }

        public Vector3 Size
        {
            get { return _size; }
            set { _size = value; SetDirty(); }
        }

        protected override Shape CreateShape()
        {
            return new BoxShape(Size * Transform.WorldScale);
        }
    }

    public sealed class SphereCollider : Collider
    {
        private Vector3 _center;
        private float _radius;

        public Vector3 Center
        {
            get { return _center; }
            set { _center = value;  }
        }

        public float Radius
        {
            get { return _radius; }
            set { _radius = value; SetDirty(); }
        }

        protected override Shape CreateShape()
        {
            return new SphereShape(Radius);
        }
    }

    public sealed class CapsuleCollider : Collider
    {
        private float _height;
        private float _radius;

        public float Height
        {
            get { return _height; }
            set { _height = value; SetDirty(); }
        }

        public float Radius
        {
            get { return _radius; }
            set { _radius = value; SetDirty(); }
        }

        protected override Shape CreateShape()
        {
            return new CapsuleShape(Height, Radius);
        }
    }

    public sealed class MeshCollider : Collider
    {
        public bool Convex;
        public Mesh Mesh;

        static Dictionary<int, ConvexHullShape> shapes = new Dictionary<int, ConvexHullShape>();

        protected override Shape CreateShape()
        {
            var hash = (Mesh.GetInstanceId(), Transform.WorldScale.X).GetHashCode();
            if (!shapes.TryGetValue(hash, out var hull))
            {
                int count = Mesh.Vertices.Length;
                var verts = Mesh.Vertices;
                List<JVector> vectors = new List<JVector>(count);
                for (int i = 0; i < count; i++)
                    vectors.Add(verts[i] * Transform.WorldScale);

                hull = new ConvexHullShape(vectors);
                shapes.Add(hash, hull);
            }

            return hull;
        }
    }

    public sealed class TerrainCollider : Collider
    {
        public Terrain Terrain;

        protected override Shape CreateShape()
        {
            var dimx = Terrain.HeightField.GetLength(0) - 1;
            var dimy = Terrain.HeightField.GetLength(1) - 1;

            var fx = Terrain.TerrainSize.X / dimx;
            var fy = Terrain.TerrainSize.Y / dimy;

            return new TerrainShape(Terrain.HeightField, fx, fy);
        }
    }
}