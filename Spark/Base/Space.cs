using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Jitter;
using Jitter.Collision;
using SharpDX;

namespace Spark
{
    public enum RenderPass
    {
        None = 0,
        Opaque = 1,
        Light = 2,
        Transparent = 3,
        Distortion = 4,
        Mirror = 5,
        Forward = 6,
        Wireframe = 7,
        Debug = 8,
        Overlay = 9,
        Shadow = 10,
        ShadowMap = 11,
        PostProcess = 12
    }

    internal class EntitySpace
    {
        internal Icoseptree VisiblityTree;
        private readonly List<IDraw> AlwaysVisible = new List<IDraw>();
        private AssetManager _content;

        public List<Entity> Entities = new List<Entity>();
        public List<Entity> EntitiesToWakeup = new List<Entity>();

        public World PhysicsWorld { get; private set; }
        public CollisionSystem CollisionSystem { get; private set; }

        public AssetManager Content
        {
            get
            {
                if (_content == null) _content = new AssetManager(Engine.Device);
                return _content;
            }
        }

        public EntitySpace()
        {
            VisiblityTree = new Icoseptree();
            CollisionSystem = new CollisionSystemPersistentSAP();
            PhysicsWorld = new World(CollisionSystem);
            PhysicsWorld.AllowDeactivation = true;
        }

        private ConcurrentStack<ISpatial> updateStack = new ConcurrentStack<ISpatial>();

        internal void Add(Entity entity)
        {
            if (entity == null) return;
            Entities.Add(entity);
            EntitiesToWakeup.Add(entity);
        }

        internal void Remove(Entity entity)
        {
            if (entity == null) return;
            Entities.Remove(entity);
            EntitiesToWakeup.Remove(entity);
        }

        internal bool Raycast(Ray ray, Entity entity, out RaycastResult result)
        {
            result = new RaycastResult();

            var body = entity.GetComponent<RigidBody>();
            if (body == null) return false;

            if (CollisionSystem.Raycast(body.JBody, ray.Position, ray.Direction * 100, out Jitter.LinearMath.JVector normal, out float fraction))
            {
                result.hitPoint = ray.Position + (ray.Direction * 100) * fraction;
                result.entity = entity;
                return true;
            }

            return false;
        }

        internal bool Raycast(Ray ray, out RaycastResult result)
        {
            result = new RaycastResult();

            try
            {
                var query = Query(ray);
                var rawresult = new RaycastResult();
                bool rawhit = false;

                List<RaycastResult> points = new List<RaycastResult>();

                foreach (var spatial in query)
                {
                    if (spatial is IMousePick pick)
                    {
                        var entity = (spatial as Component).Entity;
                        if (entity.EditorFlags.HasFlag(EditorFlags.Ghost))
                            continue;

                        if (pick.Intersects(ray, out rawresult))
                        {
                            rawresult.entity = entity;
                            rawhit = true;
                            points.Add(rawresult);
                            //break;
                            //return true;
                        }
                    }
                }

                float distance = float.MaxValue;
                foreach (var res in points)
                {
                    var d = Vector3.Distance(ray.Position, res.hitPoint);
                    if (d < distance)
                    {
                        distance = d;
                        rawresult = res;
                    }
                }

                //if (query.Count > 0)
                //{
                //    entity = (query[0] as Component).Entity;
                //    return true;
                //}

                bool physhit = false;

                if (CollisionSystem.Raycast(ray.Position, ray.Direction * 100, null, out Jitter.Dynamics.RigidBody body, out Jitter.LinearMath.JVector normal, out float fraction))
                {
                    result.hitPoint = ray.Position + (ray.Direction * 100) * fraction;
                    result.entity = body.Tag as Entity;
                    physhit = true;
                    //return true;
                }

                if (physhit && result.entity.HasComponent<Terrain>())
                {
                    if (rawhit)
                    {
                        var a = Vector3.Distance(ray.Position, rawresult.hitPoint);
                        var b = Vector3.Distance(ray.Position, result.hitPoint);

                        if (a < b)
                            result = rawresult;
                    }

                    return true;
                }

                if (rawhit)
                    result = rawresult;

                if (rawhit || physhit)
                    return true;

                return false;
            }
            catch(Exception ex)
            {
                Debug.Log(ex.Message);
                return false;
            }
        }

        internal List<ISpatial> Query(BoundingBox box)
        {
            IcoBoxQuery query = new IcoBoxQuery(box, VisiblityTree);
            List<ISpatial> result = query.Execute(null);
            return result;
        }

        internal void Query(BoundingBox box, Action<ISpatial> action)
        {
            IcoBoxVisitor query = new IcoBoxVisitor(box, VisiblityTree);
            query.Execute(action);
        }

        internal List<ISpatial> Query(Ray ray)
        {
            IcoRayQuery query = new IcoRayQuery(ray, VisiblityTree);
            List<ISpatial> result = query.Execute(null);
            return result;
        }

        internal List<ISpatial> Query(Matrix viewMatrix)
        {
            var frustum = new BoundingFrustum(viewMatrix);
            IcoFrustumQuery query = new IcoFrustumQuery(frustum, VisiblityTree);
            List<ISpatial> result = query.Execute(null);
            return result;
        }

        internal List<ISpatial> Query(Camera camera)
        {
            var frustum = new BoundingFrustum(camera.View * camera.Projection);
            IcoFrustumQuery query = new IcoFrustumQuery(frustum, VisiblityTree);
            List<ISpatial> result = query.Execute(null);
            return result;
        }


        internal void Query(Matrix viewMatrix, ref IcoQueryResult result)
        {
            var frustum = new BoundingFrustum(viewMatrix);
            IcoFrustumQueryPro query = new IcoFrustumQueryPro(frustum, VisiblityTree);
            result.AlwaysVisible = AlwaysVisible;
            query.Execute(ref result);
        }

        internal List<ISpatial> Query(Matrix viewMatrix, List<ISpatial> result)
        {
            var frustum = new BoundingFrustum(viewMatrix);
            IcoFrustumQuery query = new IcoFrustumQuery(frustum, VisiblityTree);
            query.Execute(result);
            return result;
        }

        internal List<ISpatial> Query(Camera camera, List<ISpatial> result)
        {
            var frustum = new BoundingFrustum(camera.View * camera.Projection);
            IcoFrustumQuery query = new IcoFrustumQuery(frustum, VisiblityTree);
            query.Execute(result);
            return result;
        }

        internal void UpdatePhysics(float delta)
        {
            // if (Engine.IsEditor) return;

            Profiler.Start("Physics Update");
            PhysicsWorld.Step(delta, true);
            Profiler.Stop();
        }

        internal void UpdateEntities()
        {
            Profiler.Start("WAKEUP");

            if (EntitiesToWakeup.Count > 0)
            {
                foreach (var entity in EntitiesToWakeup)
                    entity.WakeUp();

                EntitiesToWakeup.Clear();
            }

            while (updateStack.TryPop(out var changedObject))
            {
                var old = changedObject.BoundingSphere;
                changedObject.UpdateBounds();
                changedObject.SpatialNode.UpdateObject(changedObject, old.Center, old.Radius);
            }

            Profiler.Stop();
            ScriptExecution.Update();
        }

        internal void Remove(IComponent component)
        {
            if (component == null) return;

            if (component is ISpatial spatial && spatial.SpatialNode != null)
            {
                spatial.Transform.OnChanged += () =>
                {
                    updateStack.Push(spatial);
                };

                spatial.SpatialNode.RemoveObject(spatial);
                spatial.SpatialNode = null;
            }
            else if (component is IDraw draw)
            {
                AlwaysVisible.Remove(draw);
            }
        }

        internal void Insert(IComponent component)
        {
            if (component == null) return;

            if (component is ISpatial spatial)
            {
                if (spatial.SpatialNode == null)
                    spatial.SpatialNode = VisiblityTree.AddObject(spatial);

                spatial.Transform.OnChanged += () =>
                {
                    updateStack.Push(spatial);
                };
            }
            else if (component is IDraw draw)
            {
                AlwaysVisible.Add(draw);
            }
        }
    }
}