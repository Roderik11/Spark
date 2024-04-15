using System.Collections;
using System.Collections.Generic;
using System;
using System.ComponentModel;

namespace Spark
{
    public class GUIProperty
    {
        protected readonly object[] targets;
        protected readonly Field field;
        protected int hashcode = 0;

        public int Depth;

        public object Target => targets[0];

        public bool Expanded
        {
            get
            {
                if(expandedTable.TryGetValue(GetCode(), out var expanded))
                    return expanded;

                return false;
            }
            set => expandedTable[GetCode()] = value;
        }

        public virtual string Name { get { return field.Name; } }
        public virtual Type Type { get { return field.Type; } }
        public bool ReadOnly => !field.CanWrite;

        public virtual int Index { get; }

        public bool HasMixedValue
        {
            get
            {
                var result = field.GetValue(targets[0]);

                for (int i = 1; i < targets.Length; i++)
                {
                    var temp = field.GetValue(targets[i]);

                    if (!Equals(temp, result))
                        return true;
                }

                return false;
            }
        }

        public bool IsList => typeof(IList).IsAssignableFrom(Type);
        public bool IsDictionary => typeof(IDictionary).IsAssignableFrom(Type);

        public Type GetElementType()
        {
            if(IsList) return field.Type.GetGenericArguments()[0];
            return Type;
        }

        static readonly Dictionary<int, bool> expandedTable = new Dictionary<int, bool>();

        public GUIProperty(Field field, params object[] targets)
        {
            this.field = field;
            this.targets = targets;
        }

        public virtual int GetCode()
        {
            if (hashcode == 0)
                hashcode = (Depth, field.DeclaringType.GetHashCode(), 0, field.Name.GetHashCode()).GetHashCode();

            return hashcode;
        }

        public virtual object GetValue()
        {
            object result = null;

            for (int i = 0; i < targets.Length; i++)
                return field.GetValue(targets[i]);

            return result;
        }

        public virtual void SetValue(object value)
        {
            for (int i = 0; i < targets.Length; i++)
                field.SetValue(targets[i], value);
        }

        public K GetAttribute<K>() where K : Attribute
        {
            return field.GetAttribute<K>();
        }

        public GUIPropertyElement GetArrayElementAtIndex(int index)
        {
            return new GUIPropertyElement(field, index, targets) { Depth = Depth + 1 };
        }

        public void RemoveElementAtIndex(int index)
        {
            var value = GetValue();
            if (value is IList list)
                list.RemoveAt(index);
        }

        public GUIPropertyElement AddElement()
        {
            if (!IsList) return null;

            var elementType = field.Type.GetGenericArguments()[0];
            var list = GetValue() as IList;

            if (typeof(Asset).IsAssignableFrom(elementType))
                list.Add(null);
            else
            {
                var obj = Activator.CreateInstance(elementType);
                list.Add(obj);
            }

            return new GUIPropertyElement(field, list.Count - 1, targets) { Depth = Depth + 1 };
        }

        public int GetArrayLength()
        {
            var value = GetValue();
            if(value is IList list) return list.Count;
            return 0;
        }
    }

    public class GUIPropertyElement : GUIProperty
    {
        private readonly int elementIndex;
        protected readonly Type elementType;

        public override string Name { get { return $"Element {elementIndex}"; } }
        public override Type Type { get { return elementType; } }

        public override int Index => elementIndex;
             
        public GUIPropertyElement(Field field, int index, params object[] targets) : base(field, targets)
        {
            elementIndex = index;
            elementType = field.Type.GetGenericArguments()[0];
        }

        public override int GetCode()
        {
            hashcode = (Depth, field.DeclaringType.GetHashCode(), elementIndex + 1, field.Name.GetHashCode()).GetHashCode();
            return hashcode;
        }

        public override object GetValue()
        {
            object result = null;

            for (int i = 0; i < targets.Length; i++)
            {
                var list = field.GetValue(targets[i]) as IList;
                return list[elementIndex];
            }

            return result;
        }

        public override void SetValue(object value)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                var list = field.GetValue(targets[i]) as IList;
                list[elementIndex] = value;
            }
        }

    }

    public class GUIObject
    {
        static readonly Dictionary<int, bool> expandedTable = new Dictionary<int, bool>();

        private Dictionary<string, int> counts = new Dictionary<string, int>();
        private Dictionary<string, Field> allFields = new Dictionary<string, Field>();
        private Dictionary<string, GUIProperty> properties = new Dictionary<string, GUIProperty>();
        private Dictionary<string, GUIProperty> hiddenFields = new Dictionary<string, GUIProperty>();
        private object[] targets;
        
        private List<string> fieldNames = new List<string>();

        public string Name { get; private set; }
        public Type Type { get; private set; }
        public object Target => targets[0];

        public int Depth;
        public int Index;


        public bool Expanded
        {
            get
            {
                if (expandedTable.TryGetValue(GetCode(), out var expanded))
                    return expanded;

                return false;
            }
            set => expandedTable[GetCode()] = value;
        }

        private int hashcode = 0;

        public virtual int GetCode()
        {
            if (hashcode == 0)
                hashcode = (Depth, Type.GetHashCode(), Index + 1, Type.DeclaringType?.Name.GetHashCode()).GetHashCode();

            return hashcode;
        }

        public GUIObject(params object[] objects)
        {
            targets = objects;

            HashSet<string> hash = new HashSet<string>();
            int distinctTypes = 0;

            foreach (object obj in objects)
            {
                Type type = obj.GetType();
             
                if (hash.Contains(type.Name))
                    continue;

                hash.Add(type.Name);
                distinctTypes++;

                Type = type;
                Name = type.Name;

                Reflect(type);
            }

            if (distinctTypes > 1)
                Type = null;

            //fieldNames.Reverse();

            foreach(string name in fieldNames)
            {
                var field = allFields[name];
                if (field.Ignored) continue;
                if(!field.IsPublic) continue;
               
                if (counts[name] != distinctTypes) continue;

                BrowsableAttribute browsable = field.GetAttribute<BrowsableAttribute>();
                if (browsable == null && !field.CanWrite) continue;

                if (browsable != null && !browsable.Browsable)
                {
                    hiddenFields.Add(name, new GUIProperty(field, targets));
                    continue;
                }

                properties.Add(name, new GUIProperty(field, targets));
            }

            //foreach (PropertyCategory cat in Categories)
            //cat.Members.Sort((a, b) => a.Name.CompareTo(b.Name));
        }

        private void Reflect(Type type)
        {
            Mapping members = Reflector.GetMapping(type);

            foreach (var field in members)
            {
                if (!IsSerializable(field))
                    continue;

                if (!counts.ContainsKey(field.Name))
                    counts.Add(field.Name, 1);

                if(allFields.TryGetValue(field.Name, out var result))
                {
                    if (result.Type.Equals(field.Type))
                        counts[field.Name]++;
                }
                else
                {
                    allFields.Add(field.Name, field);
                    fieldNames.Add(field.Name);
                }
            }
        }

        private bool IsList(Field field) => typeof(IList).IsAssignableFrom(field.Type);

        private bool IsSerializable(Type type) => typeof(IAsset).IsAssignableFrom(type) || Reflector.GetAttribute<SerializableAttribute>(type) != null;

        private bool IsSerializable(Field field)
        {
            if (!IsList(field))
                return true;

            Type eltype = null;

            if (field.Type.HasElementType)
                eltype = field.Type.GetElementType();
            else if (field.Type.GenericTypeArguments.Length > 0)
                eltype = field.Type.GenericTypeArguments[0];
            else if (field.Type.BaseType.GenericTypeArguments.Length > 0)
                eltype = field.Type.BaseType.GenericTypeArguments[0];

            if (eltype.IsPrimitive)
                return true;
            else
                return IsSerializable(eltype);
        }

        public int ElementCount => properties.Count;

        public List<GUIProperty> GetProperties()
        {
            var list = new List<GUIProperty>(properties.Values);
            foreach (var property in list)
                property.Depth = Depth;
            return list;
        }

        public GUIProperty GetProperty(string name)
        {
            if (properties.TryGetValue(name, out GUIProperty result))
                return result;
            if (hiddenFields.TryGetValue(name, out GUIProperty result2))
                return result2;

            return null;
        }
    }
}