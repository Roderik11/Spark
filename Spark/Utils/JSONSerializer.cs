using SharpDX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark
{
    public static class JSONSerializer
    {
        static readonly Dictionary<string, JSONConverter> converters = new Dictionary<string, JSONConverter>();
        static readonly Dictionary<int, object> idToObject = new Dictionary<int, object>();
        static readonly List<ReferenceCase> referenceCases = new List<ReferenceCase>();
        static readonly EnumConverter enumConverter = new EnumConverter();

        public static JSONConverter GetConverter(Type type)
        {
            string name = type.FullName;
            JSONConverter result = null;

            if (converters.TryGetValue(name, out result))
                return result;

            return result;
        }

        struct ReferenceCase
        {
            public Field field;
            public object instance;
            public int id;

            public void SetValue(object value)
            {
                field.SetValue(instance, value);
            }
        }

        static JSONSerializer()
        {
            converters.Add(typeof(int).FullName, new IntConverter());
            converters.Add(typeof(float).FullName, new FloatConverter());
            converters.Add(typeof(double).FullName, new DoubleConverter());
            converters.Add(typeof(bool).FullName, new BoolConverter());
            converters.Add(typeof(string).FullName, new StringConverter());
            converters.Add(typeof(Vector2).FullName, new Vector2Converter());
            converters.Add(typeof(Vector3).FullName, new Vector3Converter());
            converters.Add(typeof(Vector4).FullName, new Vector4Converter());
            converters.Add(typeof(Quaternion).FullName, new QuaternionConverter());
            converters.Add(typeof(Color).FullName, new ColorConverter());
            converters.Add(typeof(Color3).FullName, new Color3Converter());
            converters.Add(typeof(Color4).FullName, new Color4Converter());
            converters.Add(typeof(Enum).FullName, new EnumConverter());
        }

        public static void ResolveReferences()
        {
            foreach (var rc in referenceCases)
            {
                if (idToObject.TryGetValue(rc.id, out var idref))
                    rc.SetValue(idref);
            }

            referenceCases.Clear();
            idToObject.Clear();
        }

        public static JSON Serialize(object obj, bool includeType = true)
        {
            var type = obj.GetType();
            var json = new JSON();
            var mapping = Reflector.GetMapping(type);
            object value = null;

            if (includeType)
                json["$type"] = type.FullName.Replace("Spark.", "!");// TypeToHash(type);

            if (obj is IDRef refid)
                json["$id"] = refid.Id;

            foreach (var field in mapping)
            {
                if (!field.CanWrite) continue;

                if (typeof(IDRef).IsAssignableFrom(field.Type))
                {
                    if (field.GetValue(obj) is IDRef reference)
                        json[field.Name] = reference.Id;
                }
                else if (typeof(IAsset).IsAssignableFrom(field.Type))
                {
                    if (field.GetValue(obj) is IAsset asset)
                        json[field.Name] = AssetDatabase.PathToGuid(asset.Path);
                }
                else if (typeof(IList).IsAssignableFrom(field.Type))
                {
                    if (field.Type.IsArray)
                    {
                        int rank = field.Type.GetArrayRank();
                        if (rank > 1) continue;
                    }

                    value = field.GetValue(obj);
                    var li = value as IList;
                    Type eltype = null;

                    if (field.Type.HasElementType)
                        eltype = field.Type.GetElementType();
                    else if (field.Type.GenericTypeArguments.Length > 0)
                        eltype = field.Type.GenericTypeArguments[0];
                    else if (field.Type.BaseType.GenericTypeArguments.Length > 0)
                        eltype = field.Type.BaseType.GenericTypeArguments[0];
                    else continue;

                    if (eltype.IsPrimitive)
                        json[field.Name] = value;// converter.ToJson(value);
                    else if (typeof(IAsset).IsAssignableFrom(eltype))
                    {
                        var list = new string[li.Count];
                        for (int i = 0; i < li.Count; i++)
                        {
                            var child = li[i] as IAsset;
                            list[i] = AssetDatabase.PathToGuid(child.Path);
                        }

                        json[field.Name] = list;// converter.ToJson(value);
                    }
                    else if (Reflector.GetAttribute<SerializableAttribute>(eltype) != null)
                    {
                        var list = new JSON[li.Count];
                        for (int i = 0; i < li.Count; i++)
                        {
                            var childValue = li[i];
                            var child = Serialize(childValue, childValue.GetType() != eltype);
                            list[i] = child;
                        }

                        json[field.Name] = list;// converter.ToJson(value);
                    }
                }
                else if (converters.TryGetValue(field.Type.FullName, out var converter))
                {
                    value = field.GetValue(obj);

                    var defaultValue = field.GetAttribute<DefaultValueAttribute>();
                    if (defaultValue != null)
                    {
                        if (!object.Equals(value, defaultValue.Value))
                            json[field.Name] = converter.ToJson(value);
                    }
                    else
                    {
                        json[field.Name] = converter.ToJson(value);
                    }
                }
                else if (field.Type.IsEnum)
                {
                    value = field.GetValue(obj);

                    var defaultValue = field.GetAttribute<DefaultValueAttribute>();
                    if (defaultValue != null)
                    {
                        if (!object.Equals(value, defaultValue.Value))
                            json[field.Name] = enumConverter.ToJson(value);
                    }
                    else
                    {
                        json[field.Name] = enumConverter.ToJson(value);
                    }
                }
                else if (field.Type.HasAttribute<SerializableAttribute>(true))
                {
                    json[field.Name] = Serialize(field.GetValue(obj));
                }
            }

            return json;
        }

        public static object Deserialize(JSON json, Type type = null)
        {
            if (type == null && !json.Contains("$type"))
                return null;

            if (json.Contains("$type"))
            {
                var hash = json.ToString("$type").Replace("!", "Spark.");
                type = Reflector.GetType(hash);// HashToType(hash);
            }

            var mapping = Reflector.GetMapping(type);
            object value = null;
            object obj = Activator.CreateInstance(type);
            object jsonValue = null;

            foreach (var field in mapping)
            {
                if (!field.CanWrite) continue;

                if (!json.Contains(field.Name))
                    continue;

                if (typeof(IDRef).IsAssignableFrom(field.Type))
                {
                    int id = json.ToInt(field.Name);
                    referenceCases.Add(new ReferenceCase
                    {
                        field = field,
                        instance = obj,
                        id = id
                    });
                }
                else if (converters.TryGetValue(field.Type.FullName, out var converter))
                {
                    jsonValue = json[field.Name];
                    value = converter.FromJson(jsonValue);
                    field.SetValue(obj, value);
                }
                else if (typeof(IList).IsAssignableFrom(field.Type))
                {
                    var list = json[field.Name] as IList;
                    var newlist = field.GetValue(obj) as IList;

                    Type eltype = null;

                    if (field.Type.HasElementType)
                        eltype = field.Type.GetElementType();
                    else if (field.Type.GenericTypeArguments.Length > 0)
                        eltype = field.Type.GenericTypeArguments[0];
                    else if (field.Type.BaseType.GenericTypeArguments.Length > 0)
                        eltype = field.Type.BaseType.GenericTypeArguments[0];

                    foreach (var element in list)
                    {
                        var deserialized = Deserialize(element as JSON, eltype);
                        if (deserialized != null) newlist.Add(deserialized);
                    }
                }
                else if (field.Type.IsEnum)
                {
                    jsonValue = json[field.Name];
                    value = enumConverter.FromJson(jsonValue);
                    field.SetValue(obj, value);
                }
                else if (field.Type.HasAttribute<SerializableAttribute>(true))
                {
                    value = Deserialize(json.ToJSON(field.Name));
                    field.SetValue(obj, value);
                }
            }

            if (obj is IDRef)
            {
                int id = json.ToInt("$id");
                idToObject.Add(id, obj);
            }

            if (obj is IOnDeserialize deserialize)
                deserialize.OnDeserialize(json);

            return obj;
        }

        public static T Deserialize<T>(JSON json)
        {
            var type = typeof(T);
            var mapping = Reflector.GetMapping(type);
            object value = null;
            T obj = Activator.CreateInstance<T>();
            object jsonValue = null;

            foreach (var field in mapping)
            {
                if (!field.CanWrite) continue;
                if (typeof(IDRef).IsAssignableFrom(field.Type))
                {
                    int id = json.ToInt(field.Name);
                    referenceCases.Add(new ReferenceCase
                    {
                        field = field,
                        instance = obj,
                        id = id
                    });
                }
                else if (typeof(IList).IsAssignableFrom(field.Type))
                {
                    var list = json[field.Name] as IList;
                    var newlist = field.GetValue(obj) as IList;

                    Type eltype = null;

                    if (field.Type.HasElementType)
                        eltype = field.Type.GetElementType();
                    else if (field.Type.GenericTypeArguments.Length > 0)
                        eltype = field.Type.GenericTypeArguments[0];
                    else if (field.Type.BaseType.GenericTypeArguments.Length > 0)
                        eltype = field.Type.BaseType.GenericTypeArguments[0];

                    foreach (var element in list)
                    {
                        var deserialized = Deserialize(element as JSON, eltype);
                        if (deserialized != null) newlist.Add(deserialized);
                    }
                }
                else if (converters.TryGetValue(field.Type.FullName, out var converter))
                {
                    jsonValue = json[field.Name];
                    value = converter.FromJson(jsonValue);
                    field.SetValue(obj, value);
                }
                else if (field.Type.HasAttribute<SerializableAttribute>(true))
                {
                    value = Deserialize(json.ToJSON(field.Name));
                    field.SetValue(obj, value);
                }
            }

            if (obj is IDRef)
            {
                int id = json.ToInt("$id");
                idToObject.Add(id, obj);
            }

            if (obj is IOnDeserialize deserialize)
                deserialize.OnDeserialize(json);

            return obj;
        }
    }

    public abstract class JSONConverter
    {
        public abstract object ToJson(object value);
        public abstract object FromJson(object value);
    }

    public class IntConverter : JSONConverter
    {
        public override object ToJson(object value)
        {
            return value;
        }

        public override object FromJson(object value)
        {
            return System.Convert.ToInt32(value);
        }
    }

    public class FloatConverter : JSONConverter
    {
        public override object ToJson(object value)
        {
            return value;
        }

        public override object FromJson(object value)
        {
            return System.Convert.ToSingle(value);
        }
    }

    public class DoubleConverter : JSONConverter
    {
        public override object ToJson(object value)
        {
            return value;
        }

        public override object FromJson(object value)
        {
            return System.Convert.ToDouble(value);
        }
    }

    public class BoolConverter : JSONConverter
    {
        public override object ToJson(object value)
        {
            return value;
        }

        public override object FromJson(object value)
        {
            return System.Convert.ToBoolean(value);
        }
    }

    public class StringConverter : JSONConverter
    {
        public override object ToJson(object value)
        {
            return value;
        }

        public override object FromJson(object value)
        {
            return System.Convert.ToString(value);
        }
    }

    public class Vector2Converter : JSONConverter
    {
        public override object ToJson(object value)
        {
            return Utils.Vector2ToString((Vector2)value);
        }

        public override object FromJson(object value)
        {
            return Utils.StringToVector2(System.Convert.ToString(value));
        }
    }

    public class Vector3Converter : JSONConverter
    {
        public override object ToJson(object value)
        {
            return Utils.Vector3ToString((Vector3)value);
        }

        public override object FromJson(object value)
        {
            return Utils.StringToVector3(System.Convert.ToString(value));
        }
    }

    public class Vector4Converter : JSONConverter
    {
        public override object ToJson(object value)
        {
            return Utils.Vector4ToString((Vector4)value);
        }

        public override object FromJson(object value)
        {
            return Utils.StringToVector4(System.Convert.ToString(value));
        }
    }

    public class ColorConverter : JSONConverter
    {
        public override object ToJson(object value)
        {
            var color = (Color)value;
            return Utils.Vector4ToString(color.ToVector4());
        }

        public override object FromJson(object value)
        {
            var vector = Utils.StringToVector4(System.Convert.ToString(value));
            return new Color(vector);
        }
    }

    public class Color3Converter : JSONConverter
    {
        public override object ToJson(object value)
        {
            return Utils.Vector3ToString((Color3)value);
        }

        public override object FromJson(object value)
        {
            return (Color3)Utils.StringToVector3(System.Convert.ToString(value));
        }
    }

    public class Color4Converter : JSONConverter
    {
        public override object ToJson(object value)
        {
            return Utils.Vector4ToString((Color4)value);
        }

        public override object FromJson(object value)
        {
            return (Color4)Utils.StringToVector4(System.Convert.ToString(value));
        }
    }

    public class QuaternionConverter : JSONConverter
    {
        public override object ToJson(object value)
        {
            return Utils.QuaternionToString((Quaternion)value);
        }

        public override object FromJson(object value)
        {
            return Utils.StringToQuaternion(System.Convert.ToString(value));
        }
    }

    public class EnumConverter : JSONConverter
    {
        public override object ToJson(object value)
        {
            return Convert.ToInt32(value);
        }

        public override object FromJson(object value)
        {
            return Convert.ToInt32(value);
        }
    }
}
