using System.Collections.Generic;
using SharpDX;

namespace Spark
{
    public static class Physics
    {
        public static bool Raycast(Ray ray, out RaycastResult result) => Entity.Space.Raycast(ray, out result);

        public static bool Raycast(Ray ray, Entity entity, out RaycastResult result) => Entity.Space.Raycast(ray, entity, out result);

        public static Jitter.World World => Entity.Space.PhysicsWorld;

        public static void Query(Matrix viewMatrix, ref IcoQueryResult result) => Entity.Space.Query(viewMatrix, ref result);

        public static void Query(Matrix viewMatrix, List<ISpatial> result) => Entity.Space.Query(viewMatrix, result);

        public static void Query(Camera camera, List<ISpatial> result) => Entity.Space.Query(camera.View * camera.Projection, result);

        public static List<ISpatial> Query(Camera camera) => Entity.Space.Query(camera);

        public static List<ISpatial> Query(Matrix viewMatrix) => Entity.Space.Query(viewMatrix);

        public static List<ISpatial> Query(Ray ray) => Entity.Space.Query(ray);

        public static List<ISpatial> Query(BoundingBox box) => Entity.Space.Query(box);

        public static void Query(BoundingBox box, System.Action<ISpatial> action) => Entity.Space.Query(box, action);
    }
}