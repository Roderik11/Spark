using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Serialization;

namespace Spark
{
    public class DataSerializer : IDisposable
    {
        public string SerializeXml(object data)
        {
            return SerializeXml(data, Formatting.None, null);
        }

        public string SerializeXml(object data, Formatting format)
        {
            return SerializeXml(data, format, null);
        }

        public string SerializeXml(object data, Type[] extraTypes)
        {
            return SerializeXml(data, Formatting.None, extraTypes);
        }

        public string SerializeXml(object data, Formatting format, Type[] extraTypes)
        {
            if (data == null)
                return string.Empty;

            Type type = data.GetType();
            XmlSerializer serializer = extraTypes == null ? new XmlSerializer(type) : new XmlSerializer(type, extraTypes);

            StringWriter stringwriter = new StringWriter();

            XmlTextWriter xmlwriter = new XmlTextWriter(stringwriter);
            xmlwriter.Formatting = format;
            xmlwriter.WriteRaw("");

            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            serializer.Serialize(xmlwriter, data, ns);

            string result = stringwriter.ToString();

            stringwriter.Close();
            serializer = null;
            xmlwriter = null;

            return result;
        }

        public byte[] SerializeBinary(object data)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, data);
            stream.Position = 0;

            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, (int)stream.Length);

            stream.Close();
            formatter = null;

            return bytes;
        }

        public T DeserializeXml<T>(string filepath, Type[] extraTypes)
        {
            return (T)DeserializeXmlFile(filepath, typeof(T), extraTypes);
        }

        public T DeserializeXml<T>(string filepath)
        {
            return DeserializeXml<T>(filepath, null);
        }

        public T DeserializeXml<T>(byte[] buffer, Type[] extraTypes)
        {
            return (T)DeserializeXmlBuffer(buffer, typeof(T), extraTypes);
        }

        public T DeserializeXml<T>(byte[] buffer)
        {
            return DeserializeXml<T>(buffer, null);
        }

        public T DeserializeXml<T>(Stream stream, Type[] extraTypes)
        {
            return (T)DeserializeXmlStream(stream, typeof(T), extraTypes);
        }

        public T DeserializeXml<T>(Stream stream)
        {
            return DeserializeXml<T>(stream, null);
        }

        public T DeserializeBinary<T>(byte[] bytes)
        {
            return (T)DeserializeBinaryBuffer(bytes);
        }

        public T DeserializeBinary<T>(string filepath)
        {
            return (T)DeserializeBinaryFile(filepath);
        }

        public object DeserializeXmlFile(string filepath, Type type, Type[] extraTypes)
        {
            if (!File.Exists(filepath))
                return null;

            try
            {
                StringReader reader = new StringReader(File.ReadAllText(filepath));//redoc.OuterXml);
                XmlSerializer serializer = extraTypes == null ? new XmlSerializer(type) : new XmlSerializer(type, extraTypes);

                return serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public object DeserializeXmlBuffer(byte[] buffer, Type type, Type[] extraTypes)
        {
            if (buffer == null)
                return null;

            try
            {
                MemoryStream stream = new MemoryStream(buffer);

                return DeserializeXmlStream(stream, type, extraTypes);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private object DeserializeXmlStream(Stream stream, Type type, Type[] extraTypes)
        {
            if (stream == null)
                return null;

            try
            {
                XmlSerializer serializer = extraTypes == null ? new XmlSerializer(type) : new XmlSerializer(type, extraTypes);

                return serializer.Deserialize(stream);
            }
            catch
            {
                return null;
            }
        }

        private object DeserializeBinaryFile(string filepath)
        {
            if (!File.Exists(filepath))
                return null;

            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream(filepath, FileMode.Open);
                return formatter.Deserialize(stream);
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        private object DeserializeBinaryBuffer(byte[] bytes)
        {
            if (bytes == null)
                return null;

            try
            {
                MemoryStream stream = new MemoryStream(bytes);
                BinaryFormatter formatter = new BinaryFormatter();

                return formatter.Deserialize(stream);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void SaveXml(object content, string filename, string path, string extension)
        {
            string str = SerializeXml(content);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            File.WriteAllText(path + "\\" + filename + "." + extension, str);
        }

        public void SaveXml(object content, string filename, Type[] extraTypes)
        {
            string str = SerializeXml(content, extraTypes);
            string dir = Path.GetDirectoryName(filename);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(filename, str);
        }

        public void SaveBinary(object obj, string filename, string path, string extension)
        {
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream(path + filename + "." + extension, FileMode.Create);
                formatter.Serialize(stream, obj);
                stream.Close();
            }
            catch (Exception exception)
            {
                throw exception;
            }
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