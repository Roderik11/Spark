using System;
using SharpDX;
using Jitter.LinearMath;
using Jitter.Collision.Shapes;
using System.Collections.Generic;
using Jitter.Collision;

namespace Spark
{
    public enum ShapeType
    {
        Box,
        Sphere,
        Capsule,
        Terrain,
        Mesh
    }

    public class RigidBody : Component, IUpdate, IDrawDebug
    {
        private bool _isStatic;
        private Jitter.Collision.Shapes.Shape JShape;
        public Jitter.Dynamics.RigidBody JBody;

        public Mesh Mesh { get; set; }
        public ShapeType Type { get; set; }
        public float Mass { get; set; }

        public bool AutoInsert = true;
        public bool DebugDraw;

        private float lastInsert;
        private bool isInWorld;

        public bool IsStatic
        {
            get
            {
                return _isStatic;
            }
            set
            {
                if (_isStatic == value)
                    return;

                _isStatic = value;

                if (JBody != null)
                    JBody.IsStatic = value;
            }
        }

        public RigidBody()
        {
            Type = ShapeType.Box;
        }

        protected override void Awake()
        {
            CreateJBody();

            Transform.OnChanged += () =>
            {
                if(IsStatic)
                {
                    JBody.Orientation = JMatrix.CreateFromQuaternion(Transform.Rotation);
                    JBody.Position = Transform.WorldPosition;
                    //if (JBody.Shape is ConvexHullShape convex)
                    //    JBody.Position -= convex.Shift;
                }
            };
        }

        public void Update()
        {
            if (!isInWorld) return;
            if (JBody == null) return;
            JBody.EnableDebugDraw = DebugDraw;
           
            if (!IsStatic && JBody.IsActive)
                Transform.Set(JBody.Position, JQuaternion.CreateFromMatrix(JBody.Orientation));
           
            if(!AutoInsert && Time.TotalTime - lastInsert > 1)
                Remove();
        }

        public void Insert()
        {
            lastInsert = Time.TotalTime;

            if (isInWorld) return;
            isInWorld = true;
            Physics.World.AddBody(JBody);
        }

        public void Remove()
        {
            if (!isInWorld) return;
            isInWorld = false;
            Physics.World.RemoveBody(JBody);
        }

        static Dictionary<int, ConvexHullShape> convexHulls = new Dictionary<int, ConvexHullShape>();
        static Dictionary<int, TriangleMeshShape> triMeshShapes = new Dictionary<int, TriangleMeshShape>();

        private void CreateJBody()
        {
            BoundingBox bb = new BoundingBox();

            if (Mesh != null)
                bb = Mesh.BoundingBox;

            Vector3 extents = bb.Size * Transform.WorldScale;

            switch (Type)
            {
                case ShapeType.Box:
                    JShape = new BoxShape(extents * 2f);
                    break;

                case ShapeType.Sphere:
                    JShape = new SphereShape((float)Math.Max(Math.Max(extents.X, extents.Y), extents.Z) - .05f);
                    break;

                case ShapeType.Capsule:
                    JShape = new CapsuleShape(extents.Y, extents.X);
                    break;

                case ShapeType.Mesh:

                    var hash = (Mesh.GetInstanceId(), Transform.WorldScale.X).GetHashCode();
                    if (!convexHulls.TryGetValue(hash, out var hull))
                    {
                        int count = Mesh.Vertices.Length;
                        var verts = Mesh.Vertices;
                        List<JVector> vectors = new List<JVector>(count);
                        for (int i = 0; i < count; i++)
                            vectors.Add(verts[i] * Transform.WorldScale);

                        hull = new ConvexHullShape(vectors);
                        convexHulls.Add(hash, hull);
                    }

                    JShape = hull;

                    //var hash = (Mesh.GetInstanceId(), Transform.WorldScale.X).GetHashCode();
                    //if (!triMeshShapes.TryGetValue(hash, out var triMesh))
                    //{
                    //    int count = Mesh.Vertices.Length;
                    //    var verts = Mesh.Vertices;
                    //    List<JVector> vectors = new List<JVector>(count);
                    //    for (int i = 0; i < count; i++)
                    //        vectors.Add(verts[i] * Transform.WorldScale);

                    //    List<TriangleVertexIndices> triangles = new List<TriangleVertexIndices>();
                    //    count = Mesh.Indices.Length / 3;
                    //    for (int i = 0; i < count; i++)
                    //    {
                    //        triangles.Add( new TriangleVertexIndices
                    //        {
                    //             I0 = i * 3,
                    //             I1 = i * 3 + 1,
                    //             I2 = i * 3 + 2,
                    //        });
                    //    }

                    //    var octree = new Octree(vectors, triangles);
                    //    triMesh = new TriangleMeshShape(octree);
                    //    triMeshShapes.Add(hash, triMesh);
                    //}


                    break;
                case ShapeType.Terrain:
                    if (Entity.GetComponent<Terrain>() is Terrain terrain)
                    {
                        var dimx = terrain.HeightField.GetLength(0) - 1;
                        var dimy = terrain.HeightField.GetLength(1) - 1;

                        var fx = terrain.TerrainSize.X / dimx;
                        var fy = terrain.TerrainSize.Y / dimy;

                        JShape = new TerrainShape(terrain.HeightField, fx, fy);
                    }

                    break;
            }

            JBody = new Jitter.Dynamics.RigidBody(JShape)
            {
                EnableDebugDraw = false,
                Tag = Entity,
                Position = Transform.Position,
                Orientation = JMatrix.CreateFromQuaternion(Transform.Rotation),
                AllowDeactivation = true,
                IsStatic = IsStatic
            };

            //if (JBody.Shape is ConvexHullShape convex)
            //    JBody.Position -= convex.Shift;

            if (AutoInsert)
                Insert();
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


    //public class RigidBody : Component
    //{
    //    public Mesh Mesh { get; set; }
    //    public bool IsStatic { get; set; }

    //    public ShapeType Type { get; set; }

    //    private PhysX.RigidActor Body;
    //    private PhysX.Shape Shape;

    //    public RigidBody()
    //    {
    //        Type = ShapeType.Box;
    //    }

    //    public override void Update()
    //    {
    //        if (Body == null)
    //            CreateBody();

    //        if (Body != null)
    //        {
    //            Shape.Flags &= ~PhysX.ShapeFlag.Visualization;
    //            Transform.Matrix = Body.GlobalPose.As<SharpDX.Matrix>();
    //        }
    //    }

    //    private void CreateBody()
    //    {
    //        if (Mesh == null) return;

    //        var material = Engine.Physics.CreateMaterial(0.7f, 0.7f, 0.1f);

    //        PhysX.Geometry geometry;

    //        if (IsStatic)
    //            Body = Engine.Physics.CreateRigidStatic();
    //        else
    //            Body = Engine.Physics.CreateRigidDynamic();

    //        SharpDX.BoundingBox bb = Mesh.BoundingBox;
    //        PhysX.Math.Vector3 extents = SharpDX.Vector3.Modulate(bb.GetExtents(), Transform.WorldScale).AsPhysX();

    //        switch (Type)
    //        {
    //            case ShapeType.Box:
    //                geometry = new PhysX.BoxGeometry(extents);
    //                Shape = Body.CreateShape(geometry, material);
    //                break;
    //            case ShapeType.Sphere:
    //                geometry = new PhysX.SphereGeometry(Math.Max(Math.Max(extents.X, extents.Y), extents.Z));
    //                Shape = Body.CreateShape(geometry, material);
    //                break;
    //            case ShapeType.Capsule:
    //                geometry = new PhysX.CapsuleGeometry(extents.X, extents.Y);
    //                Shape = Body.CreateShape(geometry, material);
    //                break;
    //        }

    //        Shape.UserData = Entity;
    //        SharpDX.Matrix matrix = Transform.Matrix;
    //        matrix.ScaleVector = SharpDX.Vector3.One;
    //        Body.GlobalPose = matrix.AsPhysX();

    //        if (!IsStatic)
    //        {
    //            PhysX.RigidDynamic dyn = Body as PhysX.RigidDynamic;
    //            dyn.AngularDamping = 1f;
    //            dyn.LinearDamping = 0.1f;

    //            dyn.SetMassAndUpdateInertia(10);
    //        }

    //        Scene.Simulation.AddActor(Body);
    //    }

    //    public override void Destroy()
    //    {
    //        if (Body != null)
    //        {
    //            Scene.Simulation.RemoveActor(Body);
    //            Body.Dispose();
    //        }
    //    }
    //}
}