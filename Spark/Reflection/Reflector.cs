using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Linq;

namespace Spark
{
    public static class Reflector
    {
        private static Dictionary<string, Assembly> Assemblies = new Dictionary<string, Assembly>();
        private static Dictionary<Type, List<Type>> TypeCache = new Dictionary<Type, List<Type>>();

        private static Dictionary<Type, Dictionary<Type, Mapping>> Cache2 = new Dictionary<Type, Dictionary<Type, Mapping>>();

        private static Dictionary<Type, Dictionary<Type, Mapping>> Cache = new Dictionary<Type, Dictionary<Type, Mapping>>();
        private static Dictionary<Type, PropertyInfo[]> Properties = new Dictionary<Type, PropertyInfo[]>();

        public static Mapping<T> GetMapping<T>(Type type) where T : Attribute
        {
            if (!Cache.TryGetValue(typeof(T), out var lookup))
            {
                lookup = new Dictionary<Type, Mapping>();
                Cache.Add(typeof(T), lookup);
            }

            if (!lookup.TryGetValue(type, out var data))
            {
                Mapping<T> map = new Mapping<T>();

                MemberInfo[] infos = type.GetMembers();
                foreach (MemberInfo info in infos)
                {
                    object[] att = info.GetCustomAttributes(typeof(T), false);
                    if (att.Length > 0)
                    {
                        Field field = new Field(info);
                        // field.Attribute = att[0] as T;

                        CategoryAttribute cat = field.GetAttribute<CategoryAttribute>();
                        if (cat != null)
                            field.Category = cat.Category;

                        DescriptionAttribute desc = field.GetAttribute<DescriptionAttribute>();
                        if (desc != null)
                            field.Description = desc.Description;

                        map.Add(field);
                    }
                }

                map.Sort((a, b) => a.Category.CompareTo(b.Category));
                lookup.Add(type, map);

                data = map;
            }

            return data as Mapping<T>;
        }

        public static Mapping GetMapping(Type type)
        {
            if (!Cache2.TryGetValue(type, out var lookup))
            {
                lookup = new Dictionary<Type, Mapping>();
                Cache2.Add(type, lookup);
            }

            if (lookup.TryGetValue(type, out var mapping))
                return mapping;

            var list = new List<Field>();

            MemberInfo[] infos = type.GetMembers();
            foreach (MemberInfo info in infos)
            {
                Field field = new Field(info);
                if (field.Type == null) continue;

                CategoryAttribute cat = field.GetAttribute<CategoryAttribute>();
                if (cat != null)
                    field.Category = cat.Category;

                DescriptionAttribute desc = field.GetAttribute<DescriptionAttribute>();
                if (desc != null)
                    field.Description = desc.Description;

                list.Add(field);
            }

            Dictionary<Type, int> depth = new Dictionary<Type, int>();

            int count = 0;
            depth[type] = count++;
            Type parent = type.BaseType;
            while (parent != null)
            {
                depth[parent] = count++;
                parent = parent.BaseType;
            }

            list = list.OrderByDescending(prop => depth[prop.DeclaringType]).ToList();
            mapping = new Mapping();
            mapping.AddRange(list);

            lookup.Add(type, mapping);
            return mapping;
        }

        public static Field GetField(Type type, string name)
        {
            var mem = type.GetMember(name);
            if (mem.Length > 0)
                return new Field(mem[0]);

            return null;
        }

        public static Field GetField<T>(string name)
        {
            var mem = typeof(T).GetMember(name);
            if (mem.Length > 0)
                return new Field(mem[0]);

            return null;
        }

        public static PropertyInfo[] GetProperties<T>()
        {
            return GetProperties(typeof(T));
        }

        public static PropertyInfo[] GetProperties(Type type)
        {
            if (!Properties.ContainsKey(type))
                Properties.Add(type, type.GetProperties());
            return Properties[type];
        }

        public static void RegisterAssemblies(params Assembly[] assemblies)
        {
            if (assemblies == null)
                return;

            foreach (Assembly assembly in assemblies)
            {
                if (!Assemblies.ContainsKey(assembly.FullName))
                    Assemblies.Add(assembly.FullName, assembly);
            }
        }

        public static bool HasAttribute<T>(this Type type, bool inherit) where T : Attribute
        {
            var atts = type.GetCustomAttributes(typeof(T), inherit);
            return atts.Length > 0;
        }

        public static T GetAttribute<T>(Type type, bool inherit) where T : Attribute
        {
            object[] atts = type.GetCustomAttributes(typeof(T), inherit);
            if (atts.Length > 0)
                return (T)atts[0];
            return null;
        }

        public static T GetAttribute<T>(Type type) where T : Attribute
        {
            return GetAttribute<T>(type, false);
        }

        static Dictionary<string, Type> typeCache = new Dictionary<string, Type>();

        public static Type GetType(string fullName)
        {
            if (typeCache.TryGetValue(fullName, out var result))
                return result;

            Assembly main = Assembly.GetAssembly(typeof(Component));
            result = main.GetType(fullName);
            if (result != null)
            {
                typeCache.Add(fullName, result);
                return result;
            }

            foreach (Assembly assembly in Assemblies.Values)
            {
                result = assembly.GetType(fullName);
                if (result != null)
                {
                    typeCache.Add(fullName, result);
                    return result;
                }
            }

            return result;
        }

        public static List<Type> GetAllTypes()
        {
            List<Type> result = new List<Type>();

            Assembly main = Assembly.GetAssembly(typeof(Entity));
            result.AddRange(main.GetTypes());

            foreach (Assembly assembly in Assemblies.Values)
            {
                if (main.FullName != assembly.FullName)
                    result.AddRange(assembly.GetExportedTypes());
            }

            return result;
        }

        public static List<Type> GetTypes(Type required)
        {
            if (TypeCache.ContainsKey(required))
                return TypeCache[required];

            List<Type> types = new List<Type>();
            List<Type> result = new List<Type>();

            Assembly main = Assembly.GetAssembly(required);
            types.AddRange(main.GetTypes());

            foreach (Assembly assembly in Assemblies.Values)
            {
                if (main.FullName != assembly.FullName)
                    types.AddRange(assembly.GetTypes());
            }

            foreach (Type type in types)
            {
                if (required.IsInterface && required.IsAssignableFrom(type))
                    result.Add(type);
                else if (type.IsSubclassOf(required))
                    result.Add(type);
            }

            TypeCache.Add(required, result);

            return result;
        }

        public static List<Type> GetTypes<T>()
        {
            return GetTypes(typeof(T));
        }

        private static Dictionary<string, object[]> Attributes = new Dictionary<string, object[]>();

        public static object[] GetAttributes<T>(Type type) where T : Attribute
        {
            string key = type.Name + typeof(T).Name;

            if (!Attributes.ContainsKey(key))
                Attributes.Add(key, type.GetCustomAttributes(typeof(T), true));

            return Attributes[key];
        }
    }
}