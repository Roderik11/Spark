using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using SharpDX;
/*
* 2013 WyrmTale Games | MIT License
*
* Based on   MiniJSON.cs by Calvin Rien | https://gist.github.com/darktable/1411710
* that was Based on the JSON parser by Patrick van Bergen | http://techblog.procurios.nl/k/618/news/view/14605/14863/How-do-I-write-my-own-parser-for-JSON.html
*
* Extended it so it includes/returns a JSON object that can be accessed using 
* indexers. also easy custom class to JSON object mapping by implecit and explicit asignment  overloading
*
* Permission is hereby granted, free of charge, to any person obtaining
* a copy of this software and associated documentation files (the
* "Software"), to deal in the Software without restriction, including
* without limitation the rights to use, copy, modify, merge, publish,
* distribute, sublicense, and/or sell copies of the Software, and to
* permit persons to whom the Software is furnished to do so, subject to
* the following conditions:
*
* The above copyright notice and this permission notice shall be
* included in all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

namespace Spark
{
    public interface ISerialize
    {
        JSON ToJSON();
        void FromJSON(JSON json);
    }

    public interface IOnDeserialize
    {
        void OnDeserialize(JSON json);
    }

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

            if(includeType)
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
                else if(field.Type.HasAttribute<SerializableAttribute>(true))
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

            if(json.Contains("$type"))
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
                else if(field.Type.HasAttribute<SerializableAttribute>(true))
                {
                    value = Deserialize(json.ToJSON(field.Name));
                    field.SetValue(obj, value);
                }
            }

            if(obj is IDRef)
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

    public class JSON
    {
        public Dictionary<string, object> fields = new Dictionary<string, object>();

        public JSON() { }
        public JSON(string text)
        {
            FromText(text);
        }

        public object this[string fieldName]
        {
            get
            {
                if (fields.TryGetValue(fieldName, out var value))
                    return value;

                return null;
            }
            set
            {
                fields[fieldName] = value;
            }
        }

        public bool Contains(string name)
        {
            return fields.ContainsKey(name);
        }

        public string ToString(string fieldName)
        {
            if(fields.TryGetValue(fieldName, out var result))
                return System.Convert.ToString(result);
                
            return string.Empty;
        }

        public int ToInt(string fieldName)
        {
            if (fields.TryGetValue(fieldName, out var result))
                return System.Convert.ToInt32(result);
         
            return 0;
        }

        public double ToDouble(string fieldName)
        {
            if (fields.TryGetValue(fieldName, out var result))
                return System.Convert.ToDouble(result);
                
            return 0;
        }

        public float ToFloat(string fieldName)
        {
            if (fields.TryGetValue(fieldName, out var result))
                return System.Convert.ToSingle(result);
        
            return 0.0f;
        }

        public bool ToBoolean(string fieldName)
        {
            if (fields.TryGetValue(fieldName, out var result))
                return System.Convert.ToBoolean(result);
            
            return false;
        }

        public string ToText()
        {
            return JSONEncoder.Serialize(this);
        }

        public void FromText(string text)
        {
            JSON json = JSONEncoder.Deserialize(text);
            if (json != null)
                fields = json.fields;
        }

        public static JSON Deserialize(string value)
        {
            return JSONEncoder.Deserialize(value);
        }

        public JSON ToJSON(string fieldName)
        {
            if (!fields.ContainsKey(fieldName))
                fields.Add(fieldName, new JSON());

            return (JSON)this[fieldName];
        }

        // get typed array out of the object as object[] 
        public T[] ToArray<T>(string fieldName)
        {
            if(fields.TryGetValue(fieldName, out var result))
            {
                if (result is IEnumerable enumerable)
                {
                    List<T> list = new List<T>();

                    foreach (object value in enumerable)
                    {
                        if (list is List<string> strList)
                            strList.Add(System.Convert.ToString(value));
                        else if (list is List<int> intList)
                            intList.Add(System.Convert.ToInt32(value));
                        else if (list is List<float> fList)
                            fList.Add(System.Convert.ToSingle(value));
                        else if (list is List<bool> bList)
                            bList.Add(System.Convert.ToBoolean(value));
                        else if (list is List<JSON> jList)
                            jList.Add((JSON)value);
                    }

                    return list.ToArray();
                }
            }

            return new T[] { };
        }



        /// <summary>
        /// This class encodes and decodes JSON strings.
        /// Spec. details, see http://www.json.org/
        ///
        /// JSON uses Arrays and Objects. These correspond here to the datatypes IList and IDictionary.
        /// All numbers are parsed to doubles.
        /// </summary>
        sealed class JSONEncoder
        {
            /// <summary>
            /// Parses the string json into a value
            /// </summary>
            /// <param name="json">A JSON string.</param>
            /// <returns>An List&lt;object&gt;, a Dictionary&lt;string, object&gt;, a double, an integer,a string, null, true, or false</returns>
            public static JSON Deserialize(string json)
            {
                // save the string for debug information
                if (json == null)
                {
                    return null;
                }

                return Parser.Parse(json);
            }

            sealed class Parser : IDisposable
            {
                const string WHITE_SPACE = " \t\n\r";
                const string WORD_BREAK = " \t\n\r{}[],:\"";

                enum TOKEN
                {
                    NONE,
                    CURLY_OPEN,
                    CURLY_CLOSE,
                    SQUARED_OPEN,
                    SQUARED_CLOSE,
                    COLON,
                    COMMA,
                    STRING,
                    NUMBER,
                    TRUE,
                    FALSE,
                    NULL
                };

                StringReader json;

                Parser(string jsonString)
                {
                    json = new StringReader(jsonString);
                }

                public static JSON Parse(string jsonString)
                {
                    using (var instance = new Parser(jsonString))
                    {
                        return (instance.ParseValue() as JSON);
                    }
                }

                public void Dispose()
                {
                    json.Dispose();
                    json = null;
                }

                JSON ParseObject()
                {
                    Dictionary<string, object> table = new Dictionary<string, object>();
                    JSON obj = new JSON();
                    obj.fields = table;

                    // ditch opening brace
                    json.Read();

                    // {
                    while (true)
                    {
                        switch (NextToken)
                        {
                            case TOKEN.NONE:
                                return null;
                            case TOKEN.COMMA:
                                continue;
                            case TOKEN.CURLY_CLOSE:
                                return obj;
                            default:
                                // name
                                string name = ParseString();
                                if (name == null) 
                                    return null;

                                // :
                                if (NextToken != TOKEN.COLON)
                                    return null;
                                
                                // ditch the colon
                                json.Read();

                                // value
                                table[name] = ParseValue();
                                break;
                        }
                    }
                }

                List<object> ParseArray()
                {
                    List<object> array = new List<object>();

                    // ditch opening bracket
                    json.Read();

                    // [
                    var parsing = true;
                    while (parsing)
                    {
                        TOKEN nextToken = NextToken;

                        switch (nextToken)
                        {
                            case TOKEN.NONE:
                                return null;
                            case TOKEN.COMMA:
                                continue;
                            case TOKEN.SQUARED_CLOSE:
                                parsing = false;
                                break;
                            default:
                                object value = ParseByToken(nextToken);

                                array.Add(value);
                                break;
                        }
                    }

                    return array;
                }

                object ParseValue()
                {
                    TOKEN nextToken = NextToken;
                    return ParseByToken(nextToken);
                }

                object ParseByToken(TOKEN token)
                {
                    switch (token)
                    {
                        case TOKEN.STRING:
                            return ParseString();
                        case TOKEN.NUMBER:
                            return ParseNumber();
                        case TOKEN.CURLY_OPEN:
                            return ParseObject();
                        case TOKEN.SQUARED_OPEN:
                            return ParseArray();
                        case TOKEN.TRUE:
                            return true;
                        case TOKEN.FALSE:
                            return false;
                        case TOKEN.NULL:
                            return null;
                        default:
                            return null;
                    }
                }

                string ParseString()
                {
                    StringBuilder s = new StringBuilder();
                    char c;

                    // ditch opening quote
                    json.Read();

                    bool parsing = true;
                    while (parsing)
                    {

                        if (json.Peek() == -1)
                        {
                            parsing = false;
                            break;
                        }

                        c = NextChar;
                        switch (c)
                        {
                            case '"':
                                parsing = false;
                                break;
                            case '\\':
                                if (json.Peek() == -1)
                                {
                                    parsing = false;
                                    break;
                                }

                                c = NextChar;
                                switch (c)
                                {
                                    case '"':
                                    case '\\':
                                    case '/':
                                        s.Append(c);
                                        break;
                                    case 'b':
                                        s.Append('\b');
                                        break;
                                    case 'f':
                                        s.Append('\f');
                                        break;
                                    case 'n':
                                        s.Append('\n');
                                        break;
                                    case 'r':
                                        s.Append('\r');
                                        break;
                                    case 't':
                                        s.Append('\t');
                                        break;
                                    case 'u':
                                        var hex = new StringBuilder();

                                        for (int i = 0; i < 4; i++)
                                        {
                                            hex.Append(NextChar);
                                        }

                                        s.Append((char)Convert.ToInt32(hex.ToString(), 16));
                                        break;
                                }
                                break;
                            default:
                                s.Append(c);
                                break;
                        }
                    }

                    return s.ToString();
                }

                object ParseNumber()
                {
                    string number = NextWord;

                    if (number.IndexOf('.') == -1)
                    {
                        Int64.TryParse(number, out var parsedInt);
                        return parsedInt;
                    }

                    Double.TryParse(number, out var parsedDouble);
                    return parsedDouble;
                }

                void EatWhitespace()
                {
                    while (WHITE_SPACE.IndexOf(PeekChar) != -1)
                    {
                        json.Read();

                        if (json.Peek() == -1)
                            break;
                    }
                }

                char PeekChar => Convert.ToChar(json.Peek());

                char NextChar => Convert.ToChar(json.Read());

                string NextWord
                {
                    get
                    {
                        StringBuilder word = new StringBuilder();

                        while (WORD_BREAK.IndexOf(PeekChar) == -1)
                        {
                            word.Append(NextChar);

                            if (json.Peek() == -1)
                            {
                                break;
                            }
                        }

                        return word.ToString();
                    }
                }

                TOKEN NextToken
                {
                    get
                    {
                        EatWhitespace();

                        if (json.Peek() == -1)
                        {
                            return TOKEN.NONE;
                        }

                        char c = PeekChar;
                        switch (c)
                        {
                            case '{':
                                return TOKEN.CURLY_OPEN;
                            case '}':
                                json.Read();
                                return TOKEN.CURLY_CLOSE;
                            case '[':
                                return TOKEN.SQUARED_OPEN;
                            case ']':
                                json.Read();
                                return TOKEN.SQUARED_CLOSE;
                            case ',':
                                json.Read();
                                return TOKEN.COMMA;
                            case '"':
                                return TOKEN.STRING;
                            case ':':
                                return TOKEN.COLON;
                            case '0':
                            case '1':
                            case '2':
                            case '3':
                            case '4':
                            case '5':
                            case '6':
                            case '7':
                            case '8':
                            case '9':
                            case '-':
                                return TOKEN.NUMBER;
                        }

                        string word = NextWord;

                        switch (word)
                        {
                            case "false":
                                return TOKEN.FALSE;
                            case "true":
                                return TOKEN.TRUE;
                            case "null":
                                return TOKEN.NULL;
                        }

                        return TOKEN.NONE;
                    }
                }
            }

            /// <summary>
            /// Converts a IDictionary / IList object or a simple type (string, int, etc.) into a JSON string
            /// </summary>
            /// <param name="json">A Dictionary&lt;string, object&gt; / List&lt;object&gt;</param>
            /// <returns>A JSON encoded string, or null if object 'json' is not serializable</returns>
            public static string Serialize(JSON obj)
            {
                return Serializer.Serialize(obj);
            }

            sealed class Serializer
            {
                StringBuilder builder;

                Serializer()
                {
                    builder = new StringBuilder();
                }

                public static string Serialize(JSON obj)
                {
                    var instance = new Serializer();

                    instance.SerializeValue(obj);

                    return instance.builder.ToString();
                }

                void SerializeValue(object value)
                {
                    if (value == null)
                        builder.Append("null");
                    else if (value is string str)
                        SerializeString(str);
                    else if (value is char)
                        SerializeString(value.ToString());
                    else if (value is bool)
                        builder.Append(value.ToString().ToLower());
                    else if (value is JSON json)
                        SerializeDictionary(json.fields);
                    else if (value is IDictionary dictionary)
                        SerializeDictionary(dictionary);
                    else if (value is IList list)
                        SerializeArray(list);
                    else
                        SerializeOther(value);
                }
                void SerializeDictionary(IDictionary obj)
                {
                    bool first = true;

                    builder.Append('{');

                    foreach (object e in obj.Keys)
                    {
                        if (!first)
                            builder.Append(',');

                        builder.AppendLine();
                        SerializeString(e.ToString());
                        builder.Append(':');
                        SerializeValue(obj[e]);

                        first = false;
                    }

                    builder.AppendLine();
                    builder.Append('}');
                }

                void SerializeArray(IList anArray)
                {
                    builder.Append('[');
                    builder.AppendLine();

                    bool first = true;

                    foreach (object obj in anArray)
                    {
                        if (!first)
                            builder.Append(',');

                        SerializeValue(obj);

                        first = false;
                    }
                  
                    builder.Append(']');
                }

                void SerializeString(string str)
                {
                    builder.Append('\"');

                    char[] charArray = str.ToCharArray();
                    foreach (var c in charArray)
                    {
                        switch (c)
                        {
                            case '"':
                                builder.Append("\\\"");
                                break;
                            case '\\':
                                builder.Append("\\\\");
                                break;
                            case '\b':
                                builder.Append("\\b");
                                break;
                            case '\f':
                                builder.Append("\\f");
                                break;
                            case '\n':
                                builder.Append("\\n");
                                break;
                            case '\r':
                                builder.Append("\\r");
                                break;
                            case '\t':
                                builder.Append("\\t");
                                break;
                            default:
                                int codepoint = Convert.ToInt32(c);
                                if ((codepoint >= 32) && (codepoint <= 126))
                                {
                                    builder.Append(c);
                                }
                                else
                                {
                                    builder.Append("\\u" + Convert.ToString(codepoint, 16).PadLeft(4, '0'));
                                }
                                break;
                        }
                    }

                    builder.Append('\"');
                }

                void SerializeOther(object value)
                {
                    if (value is float
                        || value is int
                        || value is uint
                        || value is long
                        || value is double
                        || value is sbyte
                        || value is byte
                        || value is short
                        || value is ushort
                        || value is ulong
                        || value is decimal)
                    {
                        builder.Append(value.ToString());
                    }
                    else
                    {
                        SerializeString(value.ToString());
                    }
                }
            }
        }


    }
}
