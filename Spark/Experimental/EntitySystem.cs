using Jitter;
using Jitter.Collision;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Spark
{
    public static class ComponentTypeID
    {
        private static int idCounter;

        private static class ComponentID<T> where T : Component
        {
            private static readonly int typeId;

            static ComponentID()
            {
                typeId = System.Threading.Interlocked.Increment(ref idCounter);
            }

            public static int GetId() => typeId;
            
        }

        public static int GetId<T>() where T : Component
        {
            return ComponentID<T>.GetId();
        }
    }

    public class EntityArchetypeGroup : EntitySet
    {
        public BitSet Mask = new BitSet();
    }

    public class EntityQuery
    {
        public BitSet Mask = new BitSet();

    }

    public abstract class EntitySystem
    {
        public bool Enabled = true;
        public double ElapsedTime;

        public virtual void Initialize() { }
        public virtual void Update() { }
    }

    public abstract class SystemGroup : EntitySystem { }

    public sealed class InitializationGroup : SystemGroup { }
    public sealed class SimulationGroup : SystemGroup { }
    public sealed class PresentationGroup : SystemGroup { }


    public sealed class EntityWorld
    {
        public static EntityWorld Default;

        public readonly EntityManager EntityManager;
        public readonly AssetManager ContentManager;
        public readonly World PhysicsWorld;
        public readonly CollisionSystemPersistentSAP CollisionSystem;

        private readonly SystemList Systems = new SystemList();

        internal readonly Icoseptree VisiblityTree;
        internal readonly EntitySet Entities = new EntitySet();


        static EntityWorld()
        {
            Default = new EntityWorld();
        }

        private EntityWorld()
        {
            EntityManager = new EntityManager(this);
        }

        internal void Update()
        {
            Systems.Update();
        }

        internal void Insert(Entity entity)
        {
            Entities.Add(entity);
        }

        internal void Remove(Entity entity)
        {
            Entities.Remove(entity);
        }
    }

    public class EntityManager
    {
        private readonly EntityWorld World;
        private readonly Dictionary<int, EntityArchetypeGroup> Groups;

        internal EntityManager(EntityWorld world)
        {
            World = world;
            Groups = new Dictionary<int, EntityArchetypeGroup>();
        }

        public Entity Create()
        {
            var entity = new Entity();
            World.Insert(entity);
            return entity;
        }

        public Entity Create(params Component[] components)
        {
            var entity = new Entity(components);

            int hash = entity.Archetype.GetHashCode();

            if (!Groups.TryGetValue(hash, out var group))
            {
                group = new EntityArchetypeGroup();
                Groups.Add(hash, group);
            }

            group.Add(entity);

            World.Insert(entity);
            return entity;
        }

        public void Destroy(Entity entity)
        {
            World.Remove(entity);
        }

        public T AddComponent<T>(Entity entity) where T : Component
        {
            int typeId = ComponentTypeID.GetId<T>();
            if (entity.Archetype.IsBitSet(typeId))
                return null;
            
            int hashOld = entity.Archetype.GetHashCode();
            entity.Archetype.SetBit(typeId);
            int hashNew = entity.Archetype.GetHashCode();
            ChangeGroup(entity, hashOld, hashNew);

            return default;
        }

        private void ChangeGroup(Entity entity, int hashOld, int hashNew)
        {
            if(Groups.TryGetValue(hashOld, out var oldGroup))
                oldGroup.Remove(entity);

            if (!Groups.TryGetValue(hashNew, out var newGroup))
            {
                newGroup = new EntityArchetypeGroup();
                Groups.Add(hashNew, newGroup);
            }

            newGroup.Add(entity);
        }

        public void AddComponent<T>(Entity entity, T component) where T : Component
        {
            int typeId = ComponentTypeID.GetId<T>();
            if (entity.Archetype.IsBitSet(typeId)) return;

            int hashOld = entity.Archetype.GetHashCode();
            entity.Archetype.SetBit(typeId);
            int hashNew = entity.Archetype.GetHashCode();
            ChangeGroup(entity, hashOld, hashNew);
        }

        public void RemoveComponent<T>(Entity entity) where T : Component
        {
            int typeId = ComponentTypeID.GetId<T>();
            if (!entity.Archetype.IsBitSet(typeId)) return;

            int hashOld = entity.Archetype.GetHashCode();
            entity.Archetype.ClearBit(typeId);
            int hashNew = entity.Archetype.GetHashCode();
            ChangeGroup(entity, hashOld, hashNew);
        }
    }

    public class SystemList
    {
        private readonly List<EntitySystem> systems;
        private readonly ReadOnlyCollection<EntitySystem> readOnlyList;
        private readonly System.Diagnostics.Stopwatch clock;

        public IReadOnlyList<EntitySystem> List { get { return readOnlyList; } }

        public SystemList()
        {
            clock = new System.Diagnostics.Stopwatch();
            systems = new List<EntitySystem>();
            readOnlyList = new ReadOnlyCollection<EntitySystem>(systems);
        }

        public void Add<T>(int category = 0) where T : EntitySystem, new()
        {
            var system = new T();
            system.Initialize();
            systems.Add(system);
        }

        public void Update()
        {
            foreach (var system in systems)
            {
                if (system.Enabled)
                {
                    clock.Start();
                    system.Update();
                    clock.Stop();
                    system.ElapsedTime = clock.Elapsed.TotalMilliseconds;
                    clock.Reset();
                }
                else
                {
                    system.ElapsedTime = 0;
                }
            }
        }
    }
}
