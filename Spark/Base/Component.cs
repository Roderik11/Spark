using System.ComponentModel;

namespace Spark
{
    public interface IComponent
    {
        bool Enabled { get; }
        Transform Transform { get; }
    }

    public abstract class Component : IComponent, IDRef
    {
        private bool _awake;
        private readonly int _id = IDGenerator.GetId();

        [Browsable(false)]
        public int Id => _id;

        [DefaultValue(true)]
        [Browsable(false)]
        public bool Enabled { get; set; } = true;

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