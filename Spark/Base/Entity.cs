using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Serialization;
using Assimp;
using SharpDX;
using System.Security.Cryptography;

namespace Spark
{
    [Flags]
    public enum EntityFlags
    {
        None = 0,
        DontDestroy = 1,
        HideAndDontSave = 2
    }

    [Flags]
    public enum EditorFlags
    {
        None = 0,
        Expanded = 1,
        Ghost = 2
    }

    public static class ScriptExecution
    {
        internal static List<IComponentCache> list = new List<IComponentCache>();

        internal static void Add(IComponentCache wrap)
        {
            list.Add(wrap);
        }

        public static void Update()
        {
            Profiler.Start("Entity.Update");
            for (int i = 0; i < list.Count; i++)
            {
                Profiler.Start(list[i].ToString());
                list[i].Update();
                Profiler.Stop();
            }
            Profiler.Stop();
        }
    }

    public interface IComponentCache
    {
        void Update();
    }

    public class ComponentCache<T> : IComponentCache where T : Component
    {
        public static readonly Type Type = typeof(T);
        public static readonly List<RequiresAttribute> Requires = new List<RequiresAttribute>();

        internal static readonly List<T> List;
        private static readonly Dictionary<int, int> Indices;

        private bool IsParallel;
        private bool ExecuteInEditor;

        static ComponentCache()
        {
            var atts = Reflector.GetAttributes<RequiresAttribute>(Type);

            foreach (var att in atts)
                Requires.Add(att as RequiresAttribute);

            List = new List<T>();
            Indices = new Dictionary<int, int>();

            bool isUpdating = typeof(IUpdate).IsAssignableFrom(Type);
            bool isParallel = typeof(IParallel).IsAssignableFrom(Type);
            bool executeInEditor = Reflector.GetAttribute<ExecuteInEditorAttribute>(Type) != null;

            if (!isUpdating) return;

                ScriptExecution.Add(new ComponentCache<T>
                {
                    IsParallel = isParallel,
                    ExecuteInEditor = executeInEditor
                });
        }

        static void FastRemove(int index)
        {
            int last = List.Count - 1;
            if (index > last) return;

            var a = List[index];
            var b = List[last];

            Indices[b.Entity.Id] = index;
            Indices.Remove(a.Entity.Id);

            List[index] = b;
            List[last] = a;

            List.RemoveAt(last);
        }

        public bool Contains(Entity entity) => Indices.ContainsKey(entity.Id);

        public static T Get(Entity entity)
        {
            if (Indices.TryGetValue(entity.Id, out int result))
                return List[result];

            return null;
        }

        public static bool Remove(Entity entity)
        {
            if (Indices.ContainsKey(entity.Id))
            {
                FastRemove(Indices[entity.Id]);
                return true;
            }

            return false;
        }

        public static T Add(Entity entity, T component)
        {
            if (Indices.TryGetValue(entity.Id, out int result))
                return List[result];

            Indices.Add(entity.Id, List.Count);
            component.Entity = entity;
            List.Add(component);

            return component;
        }

        public void Update()
        {
            if(Engine.IsEditor && !ExecuteInEditor) return;
            List.For((component) => { ((IUpdate)component).Update(); }, IsParallel);
        }

        public override string ToString()
        {
            return typeof(T).Name;
        }
    }

    public sealed class Entity : ISerialize, IDRef, IIndex
    {
        private readonly int _id;
        private readonly ulong _uid;

        private bool enabled;
        private readonly List<Component> components = new List<Component>();
        private readonly ReadOnlyCollection<Component> readOnlyComponents;

        private static readonly List<Camera> activeCameras = new List<Camera>();
        private static readonly ReadOnlyCollection<Camera> readonlyCameras = new ReadOnlyCollection<Camera>(activeCameras);
        private static readonly Dictionary<string, bool> toExecuteInEdtor = new Dictionary<string, bool>();
        private static Dictionary<Type, MethodInfo> methodCache = new Dictionary<Type, MethodInfo>();
        private static Dictionary<Type, MethodInfo> genericCache = new Dictionary<Type, MethodInfo>();

        internal static EntitySpace Space;

        public static List<Entity> Entities => Space.Entities;

        static Entity()
        {
            Space = new EntitySpace();
          
            List<Type> types = Reflector.GetTypes<Component>();

            foreach (Type type in types)
            {
                var executeInEditor = Reflector.GetAttribute<ExecuteInEditorAttribute>(type);
                if (executeInEditor != null)
                    toExecuteInEdtor.Add(type.FullName, true);
            }
        }

        public Entity()
        {
            readOnlyComponents = new ReadOnlyCollection<Component>(components);

            _id = IDGenerator.GetId();
            _uid = IDGenerator.GetUID();
            
            Name = "no name";
            Enabled = true;

            AddComponent<Transform>();
            Space.Add(this);
        }

        public Entity(string name)
            : this()
        {
            Name = name;
        }

        public Entity(string name, params Component[] components) : this(name)
        {
            foreach (Component component in components)
            {
                var type = component.GetType();
                AddComponent(type, component);
            }
        }

        public Entity(params Component[] components) : this()
        {
            foreach (Component component in components)
                AddComponent(component);
        }

        [Browsable(false)]
        public int Id => _id;

        public ulong UID => _uid;

        [Browsable(false)]
        internal readonly BitSet Archetype = new BitSet(1);

        [Browsable(false)]
        public bool Expanded
        {
            get => EditorFlags.HasFlag(EditorFlags.Expanded);
            set
            {
                if (value) EditorFlags |= EditorFlags.Expanded;
                else EditorFlags &= ~EditorFlags.Expanded;
            }
        }

        public bool Ghost
        {
            get => EditorFlags.HasFlag(EditorFlags.Ghost);
            set
            {
                if (value) EditorFlags |= EditorFlags.Ghost;
                else EditorFlags &= ~EditorFlags.Ghost;
            }
        }

        [DefaultValue(true), Browsable(false)]
        public bool Enabled
        {
            get
            {
                if (!enabled) return false;
                if (Transform.Parent != null) return Transform.Parent.Entity.Enabled;

                return enabled;
            }
            set
            {
                enabled = value;
            }
        }

        [DefaultValue(false)]
        [Browsable(false)]
        public bool Hidden
        {
            get
            {
                if (Flags.HasFlag(EntityFlags.HideAndDontSave))
                    return true;

                if (Transform.Parent != null)
                    return Transform.Parent.Entity.Hidden;

                return false;
            }
        }

        [Browsable(false)]
        public bool IsChildOfPrototype
        {
            get
            {
                if (Transform.Parent != null)
                    return Transform.Parent.Entity.IsPrototype || Transform.Parent.Entity.IsChildOfPrototype;
                return false;
            }
        }

        [DefaultValue("")]
        public string Name { get; set; }

        [DefaultValue(null), Browsable(false)]
        public string Prototype { get; set; }

        [DefaultValue(null), Browsable(false)]
        public object Tag { get; set; }

        [MapComponent, Browsable(false)]
        public Transform Transform { get; private set; }

        [DefaultValue(EntityFlags.None)]
        public EntityFlags Flags = EntityFlags.None;

        [DefaultValue(EditorFlags.None)]
        public EditorFlags EditorFlags = EditorFlags.None;

        public int GetIndex() => _id;

        [Browsable(false)]
        public bool IsPrototype { get { return !string.IsNullOrEmpty(Prototype); } }

        public static ReadOnlyCollection<Camera> GetActiveCameras()
        {
            activeCameras.Clear();
            foreach (var cam in ComponentCache<Camera>.List)
            {
                if (cam.Enabled && cam.IsEditor == Engine.IsEditor)
                    activeCameras.Add(cam);
            }

            activeCameras.Sort((a, b) => a.Depth.CompareTo(b.Depth));

            return readonlyCameras;
        }


        public static void DontDestroyOnLoad(Entity entity)
        {
            entity.Flags |= EntityFlags.DontDestroy;
        }

        public static void HideAndDontSave(Entity entity)
        {
            entity.Flags |= EntityFlags.HideAndDontSave;
        }

        public Component AddComponent(Type type)
        {
            MethodInfo info1 = typeof(Entity).GetMethod("AddComponent", new Type[] { });
            MethodInfo info2 = info1.MakeGenericMethod(type);
            return info2.Invoke(this, null) as Component;
        }

        private Component AddComponent(Type type, Component component)
        {
            if (!genericCache.TryGetValue(type, out var method))
            {
                MethodInfo info1 = typeof(Entity).GetMethod("AddComponentPrivate", BindingFlags.Instance | BindingFlags.NonPublic);
                method = info1.MakeGenericMethod(type);
                genericCache.Add(type, method);
            }

            return method.Invoke(this, new object[1] { component }) as Component;
        }

        private void AddComponentFromJson(Component component)
        {
            if (component == null) return;
            Type type = component.GetType();
            AddComponent(type, component);
        }

        private T AddComponentPrivate<T>(T component) where T : Component
        {
            return AddComponent(component);
        }

        public T AddComponent<T>(T component) where T : Component
        {
            if (component == null) return null;

            components.Add(component);
            component.Entity = this;
            ComponentCache<T>.Add(this, component);

            foreach (var required in ComponentCache<T>.Requires)
            {
                foreach (var req in required.Types)
                {
                    bool exists = HasComponent(req);

                    if (!exists)
                    {
                        AddComponent(req);
                    }
                }
            }

            if (component is Transform)
                Transform = component as Transform;

            return component;
        }

        public T AddComponent<T>() where T : Component, new()
        {
            var component = new T();
            return AddComponent(component);
        }

        public void Destroy()
        {
            components.ForEach(x => x.Destroy());

            Space.Remove(this);

            foreach (var component in components)
            {
                if (component is ISpatial spatial)
                    Space.Remove(spatial);
                else if (component is IDraw draw)
                    Space.Remove(draw);
            }

            components.Clear();
        }

        public T GetComponent<T>() where T : Component
        {
            return ComponentCache<T>.Get(this);
        }

        public Component GetComponent(Type type)
        {
            MethodInfo info1 = typeof(Entity).GetMethod("GetComponent", new Type[] { });
            MethodInfo info2 = info1.MakeGenericMethod(type);
            return info2.Invoke(this, null) as Component;
        }

        public ReadOnlyCollection<Component> Components => readOnlyComponents;

        public ReadOnlyCollection<Component> GetComponents() => readOnlyComponents;

        //public List<T> GetComponents<T>() where T : Component
        //{
        //    List<T> result = new List<T>();

        //    foreach (Component c in components)
        //    {
        //        if (c is T || c.GetType().IsSubclassOf(typeof(T)))
        //            result.Add(c as T);
        //    }

        //    return result;
        //}

        public List<Component> GetComponents(Type type)
        {
            List<Component> result = new List<Component>();
            foreach (Component c in components)
            {
                if (c.GetType().Equals(type) || c.GetType().IsSubclassOf(type))
                    result.Add(c);
            }

            return result;
        }

        public List<T> GetComponentsInChildren<T>() where T : Component
        {
            List<T> result = new List<T>();

            foreach (Component c in components)
            {
                if (c is T || c.GetType().IsSubclassOf(typeof(T)))
                    result.Add(c as T);
            }

            foreach(Transform child in Transform)
            {
                var more = child.Entity.GetComponentsInChildren<T>();
                result.AddRange(more);
            }

            return result;
        }

        public bool HasComponent(Type type)
        {
            return GetComponent(type) != null;
        }

        public bool HasComponent<T>() where T : Component
        {
            return ComponentCache<T>.Get(this) != null;
        }

        public bool RemoveComponent(Component instance)
        {
            if (instance == null) return false;
            instance.Destroy();

            bool removed = components.Remove(instance);

            return removed;
        }

        public override string ToString()
        {
            return (!string.IsNullOrEmpty(Name)) ? Name : base.ToString();
        }

        internal void WakeUp()
        {
            int count = components.Count;
            int i = 0;

            while (i < count)
            {
                components[i].WakeUp();
                i++;
            }


            foreach(var component in components)
            {
                if (component is ISpatial spatial)
                {
                    spatial.UpdateBounds();
                    Space.Insert(spatial);
                }
                else if (component is IDraw draw)
                    Space.Insert(draw);
            }
        }

        public JSON ToJSON()
        {
            JSON json = JSONSerializer.Serialize(this);

            var list = new JSON[components.Count];
            for (int i = 0; i < components.Count; i++)
            {
                var child = JSONSerializer.Serialize(components[i]);
                list[i] = child;
            }

            json["components"] = list;
            return json;
        }

        public void FromJSON(JSON json)
        {
            JSON[] array = json.ToArray<JSON>("components");

            RemoveComponent(Transform);

            foreach (JSON e in array)
            {
                if (JSONSerializer.Deserialize(e) is Component comp)
                    AddComponentFromJson(comp);
            }
        }
    }
}