using System;
using System.ComponentModel;
using System.Net.Security;
using System.Security.Cryptography;
using SharpDX;

namespace Spark
{
    public interface IMousePick
    {
        bool Intersects(Ray ray, out RaycastResult result);
    }

    public struct RaycastResult
    {
        public Entity entity;
        public Vector3 hitPoint;
        public int meshPart;
    }

    public interface IParallel { }

    public interface IUpdate : IComponent
    {
        void Update();
    }

    public interface IDraw : IComponent
    {
        void Draw();
    }

    public interface IDrawDebug : IComponent 
    {
        void DrawDebug();
    }

    public interface IComponent
    {
        bool Enabled { get; }
        Transform Transform { get; }
    }

    public static class IDGenerator
    {
        private static int _idCounter;
        private static RandomNumberGenerator rng;

        static IDGenerator()
        {
           rng = RandomNumberGenerator.Create();
        }

        public static int GetId()
        {
            return System.Threading.Interlocked.Increment(ref _idCounter);
        }

        public static ulong GetUID()
        {
            var bytes = new byte[8];
            rng.GetBytes(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }
    }

    public interface IDRef
    {
        int Id { get; }
    }

    public abstract class Component : IComponent, IDRef
    {
        private bool _awake;
        private bool _enabled = true;
        private int _id = -1;

        [Browsable(false)]
        public int Id
        {
            get
            {
                if (_id < 0)
                    _id = IDGenerator.GetId();

                return _id;
            }
        }

        [DefaultValue(true)]
        [Browsable(false)]
        public bool Enabled { get { return _enabled; } set { _enabled = value; } }

        [Browsable(false)]
        public Entity Entity { get; internal set; }

        [Browsable(false)]
        public Transform Transform { get { return Entity != null ? Entity.Transform : null; } }


        internal void WakeUp()
        {
            if (_awake) return;
            _awake = true;
            Awake();
        }

        protected virtual void Awake() { }

        public virtual void Destroy() { }

    }
}