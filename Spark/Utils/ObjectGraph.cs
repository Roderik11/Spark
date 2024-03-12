using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Spark
{
    public class ObjectGraph : IDisposable
    {
        public class Node
        {
            [XmlAttribute]
            public string Key;

            [XmlAttribute]
            public string Name;

            public List<Node> Nodes = new List<Node>();

            [XmlAttribute]
            public int Reference;

            [XmlAttribute]
            public int RefID;

            [XmlAttribute]
            public string Type;

            [XmlAttribute]
            public string Value;

            [XmlIgnore]
            public bool WriteType;
        }

        public Node Root;
        public bool FlattenPrototypes { get; set; }

        public T Deserialize<T>(string path)
        {
            ParseFile(path);
            ReadCache.Clear();

            return (T)Deserialize(Root, typeof(T), null);
        }

        public T Deserialize<T>(string path, HashSet<IPreloadable> resources)
        {
            ParseFile(path);
            ReadCache.Clear();

            return (T)Deserialize(Root, typeof(T), resources);
        }

        public T Deserialize<T>(byte[] data)
        {
            ParseStream(data);
            ReadCache.Clear();

            return (T)Deserialize(Root, typeof(T), null);
        }

        public T Deserialize<T>(byte[] data, HashSet<IPreloadable> resources)
        {
            ParseStream(data);
            ReadCache.Clear();

            T result = (T)Deserialize(Root, typeof(T), resources);
            return result;
        }

        public HashSet<IPreloadable> GetRelatedResources(object data)
        {
            HashSet<IPreloadable> set = new HashSet<IPreloadable>();
            Root = CreateNode(data, false, false, set);
            return set;
        }

        public string Serialize(object data)
        {
            CreateTree(data);

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            settings.OmitXmlDeclaration = true;
            settings.NewLineHandling = NewLineHandling.Entitize;

            StringBuilder output = new StringBuilder();

            XmlWriter writer = XmlWriter.Create(output, settings);

            Write(Root, writer);

            writer.Flush();
            writer.Close();
            writer = null;

            return output.ToString();
        }

        private Stack<Node> Elements;
        private int Increment;
        private Dictionary<string, Prototype> Protos = new Dictionary<string, Prototype>();
        private Dictionary<int, object> ReadCache = new Dictionary<int, object>();
        private Dictionary<object, Node> WriteCache = new Dictionary<object, Node>();

        private Node CreateNode(object data, bool writeType, bool useCache, HashSet<IPreloadable> resources)
        {
            Type type = data.GetType();

            if (resources != null && data is IPreloadable)
            {
                IPreloadable res = data as IPreloadable;

                if (!resources.Contains<IPreloadable>(res))
                    resources.Add(res);
            }

            Node result = new Node { Name = type.Name, Type = type.FullName, WriteType = writeType };

            if (WriteCache.ContainsKey(data))
            {
                if (useCache)
                    return WriteCache[data];

                if (WriteCache[data].RefID == 0)
                {
                    Increment++;
                    WriteCache[data].RefID = Increment;
                }

                result.WriteType = false;
                result.Reference = WriteCache[data].RefID;
                return result;
            }
            else
            {
                WriteCache.Add(data, result);
            }

            var properties = Reflector.GetMapping(type);// Reflector.GetProperties(type);// type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (properties.Count < 1) result.Value = data.ToString();

            foreach (var info in properties)
            {
                if (info.Ignored) continue;
                if (!info.CanWrite) continue;
                //if (info.GetSetMethod() == null) continue;

                object value;

                try
                {
                    value = info.GetValue(data);
                }
                catch { continue; }

                if (value == null) continue;

                // check vs. default value
                object[] attributes = info.GetCustomAttributes(typeof(DefaultValueAttribute), true);
                if (attributes.Length > 0)
                {
                    DefaultValueAttribute def = attributes[0] as DefaultValueAttribute;
                    if (def.Value != null)
                    {
                        if (def.Value.Equals(value))
                            continue;
                    }
                }

                Type valueType = value.GetType();

                if (info.Type.IsPrimitive || info.Type == typeof(string) || info.Type.IsEnum)
                {
                    FlagsAttribute flags = Reflector.GetAttribute<FlagsAttribute>(info.Type);

                    if (flags != null)
                    {
                        Node sub = new Node { Name = info.Name, Value = ((int)value).ToString() };
                        result.Nodes.Add(sub);
                    }
                    else
                    {
                        Node sub = new Node { Name = info.Name, Value = value.ToString() };
                        result.Nodes.Add(sub);
                    }
                }
                else if (valueType.IsValueType)
                {
                    TypeConverter converter = System.ComponentModel.TypeDescriptor.GetConverter(valueType);
                    if (converter != null)
                    {
                        try
                        {
                            string str = converter.ConvertToString(value);
                            Node sub = new Node { Name = info.Name, Value = str };
                            result.Nodes.Add(sub);
                        }
                        catch (Exception exc)
                        {
                            //throw exc;
                        }
                    }
                    else
                    {
                        Node sub = CreateNode(value, !info.Type.FullName.Equals(valueType.FullName), false, resources);
                        sub.Name = info.Name;
                        result.Nodes.Add(sub);
                    }
                }
                else if(value is Array array && array.Rank > 1)
                {

                }
                else if (value is IList)
                {
                    Node sub = new Node { Name = info.Name, Type = valueType.FullName };
                    result.Nodes.Add(sub);

                    string name = null;
                    string fullname = null;

                    if (valueType.IsGenericType || valueType.BaseType.IsGenericType)
                    {
                        Type t = valueType.IsGenericType ? valueType : valueType.BaseType;

                        Type[] gens = t.GetGenericArguments();
                        if (gens.Length > 0)
                        {
                            name = gens[0].Name;
                            fullname = gens[0].FullName;
                        }
                    }
                    else
                        fullname = ((IList)value)[0].GetType().FullName;

                    foreach (object item in ((IList)value))
                    {
                        if (item != null)
                        {
                            Type stype = item.GetType();
                            Node child = CreateNode(item, !fullname.Equals(item.GetType().FullName), false, resources);

                            if (!string.IsNullOrEmpty(name))
                                child.Name = name;

                            sub.Nodes.Add(child);
                        }
                        else
                        {
                            Node child = new Node { Name = name };
                            sub.Nodes.Add(child);
                        }
                    }
                }
                else if (value is IDictionary)
                {
                    Node sub = new Node { Name = info.Name, Type = valueType.FullName };
                    result.Nodes.Add(sub);

                    Type itemType = null;

                    if (valueType.IsGenericType)
                    {
                        Type[] gens = valueType.GetGenericArguments();
                        if (gens.Length > 0)
                            itemType = gens[1];
                    }

                    IDictionary dict = (IDictionary)value;

                    foreach (object key in dict.Keys)
                    {
                        Node element = CreateNode(dict[key], !dict[key].GetType().FullName.Equals(itemType.FullName), false, resources);
                        element.Key = key.ToString();
                        sub.Nodes.Add(element);
                    }
                }
                else if (value is Entity)// && info.GetAttribute<NoRefAttribute>() == null)
                {
                    // entity as a property - we want a reference here, not the actual entity
                    Node sub = new Node { Name = info.Name, WriteType = false };

                    if (WriteCache.ContainsKey(value))
                        sub.Reference = WriteCache[value].RefID;
                    else
                    {
                        Node cached = CreateNode(value, false, false, resources);

                        if (WriteCache[value].RefID == 0)
                        {
                            Increment++;
                            WriteCache[value].RefID = Increment;
                        }

                        sub.Reference = cached.RefID;
                    }

                    result.Nodes.Add(sub);
                }
                else
                {
                    Node sub = CreateNode(value, !info.Type.FullName.Equals(valueType.FullName), false, resources);
                    sub.Name = info.Name;
                    result.Nodes.Add(sub);
                }
            }

            if (data is Entity)
            {
                Entity entity = data as Entity;

                if (!entity.IsPrototype)
                {
                    Node sub = new Node { Name = "Components", Type = typeof(Component).FullName };
                    result.Nodes.Add(sub);

                    foreach (Component c in entity.GetComponents())
                    {
                        Node child = CreateNode(c, true, false, resources);
                        child.Name = "Component";
                        sub.Nodes.Add(child);
                    }

                    //if (entity.Count > 0)
                    //{
                    //    sub = new Node { Name = "Children", Type = typeof(Entity).FullName };
                    //    result.Nodes.Add(sub);

                    //    foreach (Entity e in entity)
                    //        sub.Nodes.Add(CreateNode(e, false, true, resources));
                    //}
                }
                else
                {
                    Node sub = new Node { Name = "Components", Type = typeof(Component).FullName };
                    result.Nodes.Add(sub);
                    Node child = CreateNode(entity.Transform, true, false, resources);
                    child.Name = "Component";
                    sub.Nodes.Add(child);
                }
            }

            return result;
        }

        private void CreateTree(object data)
        {
            WriteCache.Clear();
            Root = CreateNode(data, false, false, null);
            WriteCache.Clear();
        }

        private object Deserialize(Node node, Type propertyType, HashSet<IPreloadable> resources)
        {
            if (node.Reference != 0)
            {
                if (ReadCache.ContainsKey(node.Reference))
                    return ReadCache[node.Reference];
                else
                {
                    Node refnode = FindNode(Root, node.Reference);
                    if (refnode != null)
                        return Deserialize(refnode, propertyType, resources);
                }
            }

            Type type = null;

            if (!string.IsNullOrEmpty(node.Type))
            {
                if (node.Type.Contains(":") || node.Type.Contains("."))
                {
                    string[] arr = node.Type.Split(new char[2] { ':', '.' }, StringSplitOptions.RemoveEmptyEntries);

                    type = Reflector.GetType(node.Type);
                }
                else
                {
                    type = Type.GetType(node.Type);
                }
            }
            else
                type = propertyType;

            if (type == null)
                return null;

            object result = null;
            object value = null;

            result = Activator.CreateInstance(type);

            if (!(result is IList || result is IDictionary))
            {
                var properties = Reflector.GetMapping(type);

                foreach (var info in properties)
                {
                    if (info.Ignored) continue;
                    if (!info.CanWrite) continue;

                    Node child = node.Nodes.Find(x => x.Name.Equals(info.Name));
                    if (child == null) continue;

                    if (!string.IsNullOrEmpty(child.Value))
                    {
                        TypeConverter converter = System.ComponentModel.TypeDescriptor.GetConverter(info.Type);
                        if (converter != null)
                            value = converter.ConvertFromString(child.Value);
                    }
                    else
                    {
                        try
                        {
                            value = Deserialize(child, info.Type, resources);
                        }
                        catch (Exception e)
                        {
                            value = null;
                        }
                    }

                    info.SetValue(result, value);
                }
            }

            #region special entity case

            if (result is Entity)
            {
                Entity entity = result as Entity;

                // [Prototyping]
                if (entity.IsPrototype)
                {
                    try
                    {
                        if (!Protos.ContainsKey(entity.Prototype))
                        {
                            if (resources != null)
                            {
                                HashSet<IPreloadable> sub = new HashSet<IPreloadable>();

                                Prototype proto = new Prototype { Filename = entity.Prototype };
                                //proto.Load(sub);

                                Protos.Add(entity.Prototype, proto);

                                foreach (IPreloadable res in sub)
                                {
                                    if (!resources.Contains<IPreloadable>(res)) //, Resource.PreloadComparer))
                                        resources.Add(res);
                                }
                            }
                            else
                            {
                                Prototype proto = new Prototype { Filename = entity.Prototype };
                                Protos.Add(entity.Prototype, proto);
                            }
                        }

                        if (Protos.ContainsKey(entity.Prototype))
                        {
                            result = Protos[entity.Prototype].CreateInstance();

                            if (result != null)
                            {
                                if (FlattenPrototypes)
                                {
                                    ((Entity)result).Prototype = string.Empty;
                                }

                                Node child = node.Nodes.Find(x => x.Name.Equals("Components"));
                                if (child != null)
                                {
                                    foreach (Node c in child.Nodes)
                                    {
                                        value = Deserialize(c, typeof(Component), resources);
                                        if (value != null)
                                        {
                                            ((Entity)result).AddComponent(value as Component);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // something went wrong reading the prototype
                    }
                }
                else
                {
                    Node child = node.Nodes.Find(x => x.Name.Equals("Components"));
                    if (child != null)
                    {
                        foreach (Node c in child.Nodes)
                        {
                            value = Deserialize(c, typeof(Component), resources);
                            if (value != null)
                            {
                                ((Entity)result).AddComponent(value as Component);
                            }
                        }
                    }

                    child = node.Nodes.Find(x => x.Name.Equals("Children"));
                    if (child != null)
                    {
                        foreach (Node c in child.Nodes)
                        {
                            value = Deserialize(c, typeof(Entity), resources);
                            if (value != null)
                            {
                                (value as Entity).Transform.Parent = (result as Entity).Transform;
                                // ((Entity)result).AddChild(value as Entity);
                            }
                        }
                    }
                }
            }

            #endregion special entity case

            if (result is IList)
            {
                Type itemType = null;
                if (type.IsGenericType)
                {
                    Type[] gens = type.GetGenericArguments();
                    if (gens.Length > 0)
                        itemType = gens[0];
                }

                foreach (Node child in node.Nodes)
                {
                    ((IList)result).Add(Deserialize(child, itemType, resources));
                }
            }

            if (result is IDictionary)
            {
                Type keyType = null;
                Type itemType = null;

                if (type.IsGenericType)
                {
                    Type[] gens = type.GetGenericArguments();
                    if (gens.Length > 1)
                    {
                        keyType = gens[0];
                        itemType = gens[1];
                    }
                }

                TypeConverter converter = System.ComponentModel.TypeDescriptor.GetConverter(keyType);
                if (converter != null)
                {
                    foreach (Node child in node.Nodes)
                    {
                        object key = converter.ConvertFromString(child.Key);
                        ((IDictionary)result).Add(key, Deserialize(child, itemType, resources));
                    }
                }
            }

            if (resources != null)
            {
                if (result is IPreloadable)
                {
                    if (!resources.Contains<IPreloadable>(result as IPreloadable))//, Resource.PreloadComparer))
                        resources.Add(result as IPreloadable);
                }
            }

            if (node.RefID != 0)
            {
                if (!ReadCache.ContainsKey(node.RefID))
                    ReadCache.Add(node.RefID, result);
            }

            return result;
        }

        private Node FindNode(Node parent, int reference)
        {
            Node found = parent.Nodes.Find(x => x.RefID == reference);
            if (found != null) return found;

            foreach (Node sub in parent.Nodes)
            {
                found = FindNode(sub, reference);
                if (found != null)
                    break;
            }
            return found;
        }

        private void ParseFile(string path)
        {
            Elements = new Stack<Node>();

            XmlTextReader reader = new XmlTextReader(path);
            Root = ParseNode(reader);

            reader.Close();
            reader = null;
        }

        private Node ParseNode(XmlTextReader reader)
        {
            Node node = null;

            while (!reader.EOF)
            {
                reader.Read();
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:

                        node = new Node { Name = reader.LocalName };

                        if (Elements.Count == 0)
                        {
                            Root = node;
                            Elements.Push(node);
                        }
                        else
                        {
                            Node parent = Elements.Peek();
                            parent.Nodes.Add(node);

                            if (reader.IsEmptyElement)
                            {
                                if (reader.HasAttributes)
                                {
                                    while (reader.MoveToNextAttribute())
                                    {
                                        if (reader.Name.Equals("xtype"))
                                            node.Type = reader.Value;
                                        else if (reader.Name.Equals("xkey"))
                                            node.Key = reader.Value;
                                        else if (reader.Name.Equals("xrefid"))
                                            node.RefID = Convert.ToInt32(reader.Value);
                                        else if (reader.Name.Equals("xref"))
                                            node.Reference = Convert.ToInt32(reader.Value);
                                        else
                                        {
                                            Node sub1 = new Node { Name = reader.Name, Value = reader.Value };
                                            node.Nodes.Add(sub1);
                                        }
                                    }
                                }

                                break;
                            }
                            else
                                Elements.Push(node);
                        }

                        if (reader.HasAttributes)
                        {
                            while (reader.MoveToNextAttribute())
                            {
                                if (reader.Name.Equals("xtype"))
                                    node.Type = reader.Value;
                                else if (reader.Name.Equals("xkey"))
                                    node.Key = reader.Value;
                                else if (reader.Name.Equals("xrefid"))
                                    node.RefID = Convert.ToInt32(reader.Value);
                                else if (reader.Name.Equals("xref"))
                                    node.Reference = Convert.ToInt32(reader.Value);
                                else
                                {
                                    Node sub1 = new Node { Name = reader.Name, Value = reader.Value };
                                    node.Nodes.Add(sub1);
                                }
                            }
                        }

                        break;

                    case XmlNodeType.Attribute:
                        Node sub2 = new Node { Name = reader.Name, Value = reader.Value };
                        node.Nodes.Add(sub2);
                        break;

                    case XmlNodeType.EndElement:
                        Elements.Pop();
                        break;

                    case XmlNodeType.Text:
                        node.Value = reader.Value;
                        break;

                    case XmlNodeType.CDATA:
                        node.Value = reader.Value;
                        break;

                    default:
                        break;
                }
            }
            return Root;
        }

        private void ParseStream(byte[] data)
        {
            Elements = new Stack<Node>();

            MemoryStream stream = new MemoryStream(data);
            XmlTextReader reader = new XmlTextReader(stream);
            Root = ParseNode(reader);

            stream.Close();
            reader.Close();

            reader = null;
            stream = null;
        }

        private void Write(Node node, XmlWriter writer)
        {
            writer.WriteStartElement(node.Name);

            if (node.WriteType) writer.WriteAttributeString("xtype", node.Type);
            if (!string.IsNullOrEmpty(node.Key)) writer.WriteAttributeString("xkey", node.Key);
            if (node.RefID != 0) writer.WriteAttributeString("xrefid", node.RefID.ToString());
            if (node.Reference != 0) writer.WriteAttributeString("xref", node.Reference.ToString());

            if (node.Value != null)
                writer.WriteValue(node.Value);
            else
            {
                foreach (Node sub in node.Nodes)
                    Write(sub, writer);
            }

            writer.WriteEndElement();
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposed)
        {
        }

        #endregion IDisposable Members
    }
}