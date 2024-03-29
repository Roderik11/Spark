﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Text.RegularExpressions;
using System.Diagnostics;
using SharpDX;

namespace Spark
{
    public static class XmlUtil
    {
        /// <summary>
        /// Parses an array of floats from the given string using invariant culture info,
        /// that is, basically, numbers with "." as floating point sign (e.g. 1.7 
        /// rather than 1,7).
        /// </summary>
        /// <param name="value">string containing float numbers</param>
        /// <returns>array of parsed floats</returns>
        public static float[] ParseFloats(string value)
        {
            string[] parts = Regex.Split(value.Trim(), @"\s+");
            float[] values = new float[parts.Length];

            for (int i = 0; i < parts.Length; ++i)
            {
                values[i] = float.Parse(parts[i], CultureInfo.InvariantCulture);
            }

            return values;
        }

        /// <summary>
        /// Parses names or ids from a string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string[] ParseNames(string value)
        {
            string[] parts = Regex.Split(value.Trim(), @"\s+");
            return parts;
        }

        /// <summary>
        /// Parses an array of integers from the given string using invariant culture info.
        /// </summary>
        /// <param name="value">string containing int numbers</param>
        /// <returns>array of parsed integers</returns>
        public static int[] ParseInts(string value)
        {
            string[] parts = Regex.Split(value.Trim(), @"\s+");
            int[] values = new int[parts.Length];

            for (int i = 0; i < parts.Length; ++i)
            {
                values[i] = Int32.Parse(parts[i], CultureInfo.InvariantCulture);
            }

            return values;
        }

        public static Matrix ParseMatrix(string value)
        {
            float[] values = ParseFloats(value);

            Matrix matrix = new Matrix(values[0], values[4], values[8], values[12],
                values[1], values[5], values[9], values[13],
                values[2], values[6], values[10], values[14],
                values[3], values[7], values[11], values[15]);

            return matrix;
        }

        public static Vector3 ParseVector3(string value)
        {
            float[] values = ParseFloats(value);
            return new Vector3(values[0], values[1], values[2]);
        }

        public static Vector4 ParseVector4(string value)
        {
            float[] values = ParseFloats(value);
            return new Vector4(values[0], values[1], values[2], values[3]);
        }


        /// <summary>
        /// XmlNode extensions that allows to search for nodes by id.
        /// This is a replacement for Document.GetElementById and the XPath id() function,
        /// both of which only work when a DTD (xmlns) is set.        
        /// </summary>
        /// <param name="node">Any XmlNode</param>
        /// <param name="id">ID of an element in the same document</param>
        /// <returns>XmlNode with given id or null, if none is found</returns>
        public static XmlNode SelectId(this XmlNode node, string id)
        {
            return node.OwnerDocument.DocumentElement.SelectSingleNode("//*[@id='" + id + "']");
        }

        /// <summary>
        /// Tries to get the value of node's attribute called "name".
        /// If the attribute exists its value is stored in output and
        /// true is returned. If the node is null or the attribute does
        /// not exist, false is returned and output is initialized with
        /// the default string value.
        /// </summary>
        /// <param name="node">An XML node</param>
        /// <param name="name">Attribute name</param>
        /// <param name="output">Variable to store result in</param>
        /// <returns>True if the attribute existed</returns>
        public static bool TryGetAttribute(this XmlNode node, string name, out string output)
        {
            if (node == null || node.Attributes == null || node.Attributes[name] == null)
            {
                output = default(string);
                return false;
            }

            XmlAttribute attr = node.Attributes[name];
            output = attr.Value;
            return true;
        }

        /// <summary>
        /// Tries to get the value of node's attribute called "name".
        /// If the attribute exists its value is stored in output and
        /// true is returned. If the node is null or the attribute does
        /// not exist, false is returned and output is initialized with
        /// the default integer value.
        /// </summary>
        /// <param name="node">An XML node</param>
        /// <param name="name">Attribute name</param>
        /// <param name="output">Variable to store result in</param>
        /// <returns>True if the attribute existed</returns>
        public static bool TryGetAttribute(this XmlNode node, string name, out int output)
        {
            if (node == null || node.Attributes == null || node.Attributes[name] == null)
            {
                output = default(int);
                return false;
            }

            XmlAttribute attr = node.Attributes[name];
            output = int.Parse(attr.Value);
            return true;
        }

        /// <summary>
        /// Returns the string value of node's attribute called "name".
        /// If the attribute does not exist the default value for string
        /// is returned.
        /// </summary>
        /// <param name="node">XML node</param>
        /// <param name="name">attribute name</param>
        /// <returns>Value</returns>
        public static string GetAttributeString(this XmlNode node, string name)
        {
            string value;
            TryGetAttribute(node, name, out value);
            return value;
        }

        /// <summary>
        /// Returns the int value of node's attribute called "name".
        /// If the attribute does not exist the default value for int is returned
        /// </summary>
        /// <param name="node">XML node</param>
        /// <param name="name">attribute name</param>
        /// <returns>Value</returns>
        public static int GetAttributeInt(this XmlNode node, string name)
        {
            int value;
            TryGetAttribute(node, name, out value);
            return value;
        }
    }


    /// <summary>
    /// Data wrapper for COLLADA XML sources.
    /// Gives direct access to data in a format that corresponds to the data type
    /// defined in the accessor, e.g. Vector2, Vector3, Matrix, float etc.
    /// </summary>
    public class ColladaSource
    {
        private int _count;
        private int _stride;

        private Object _data;
        private Type _dataType;
        private string _colladaType;

        /// <summary>
        /// Number of items in this source
        /// </summary>
        public int Count { get { return _count; } }

        /// <summary>
        /// Type of data within list
        /// </summary>
        public Type DataType { get { return _dataType; } }

        /// <summary>
        /// List of data contained by this node
        /// </summary>
        public Object Data { get { return _data; } }

        /// <summary>
        /// Type as specified in COLLADA file. For example "idref", "sidref",
        /// "float", "float4x4" etc.
        /// </summary>
        public string ColladaType { get { return _colladaType; } }

        /// <summary>
        /// Creates a wrapper for source data. Reads the data from the Xml node
        /// and stores it in a list of a suitable type.
        /// </summary>
        /// <param name="xmlSource">XML source node</param>
        public ColladaSource(XmlNode xmlSource)
        {
            XmlNode xmlAccessor = xmlSource.SelectSingleNode("technique_common/accessor");
            _count = int.Parse(xmlAccessor.Attributes["count"].Value);
            _stride = xmlAccessor.Attributes["stride"] != null
                ? int.Parse(xmlAccessor.Attributes["stride"].Value) : 1;

            string dataId = xmlAccessor.Attributes["source"].Value.Substring(1);
            XmlNode xmlData = xmlSource.SelectSingleNode("./*[@id='" + dataId + "']");

            // First parameter type
            XmlNode xmlFirstParam = xmlAccessor.SelectNodes("param")[0];
            string type = xmlFirstParam.Attributes["type"].Value.ToLower();

            if (xmlData.Name.ToLower().Equals("idref_array") && type == "name")
            {
                type = "idref";
            }
            else if (xmlData.Name.ToLower().Equals("sidref_array") && type != "sidref")
            {
                type = "sidref";
            }

            _colladaType = type;

            switch (type)
            {
                case "float":
                    {
                        float[] data = XmlUtil.ParseFloats(xmlData.InnerText);

                        if (_stride == 1)
                        {
                            _data = new List<float>(data);
                            _dataType = typeof(float);
                        }
                        else if (_stride == 2)
                        {
                            List<Vector2> vectors = new List<Vector2>(_count);

                            for (int i = 0; i < _count; i++)
                            {
                                Vector2 v = new Vector2(data[_stride * i + 0], data[_stride * i + 1]);
                                vectors.Add(v);
                            }

                            _data = vectors;
                            _dataType = typeof(Vector2);
                        }
                        else if (_stride == 3)
                        {
                            List<Vector3> vectors = new List<Vector3>(_count);

                            for (int i = 0; i < _count; i++)
                            {
                                Vector3 v = new Vector3(data[_stride * i + 0],
                                                        data[_stride * i + 1],
                                                        data[_stride * i + 2]);
                                vectors.Add(v);
                            }

                            _data = vectors;
                            _dataType = typeof(Vector3);
                        }
                        else if (_stride == 4)
                        {
                            List<Vector4> vectors = new List<Vector4>(_count);

                            for (int i = 0; i < _count; i++)
                            {
                                Vector4 v = new Vector4(data[_stride * i + 0],
                                                        data[_stride * i + 1],
                                                        data[_stride * i + 2],
                                                        data[_stride * i + 3]);
                                vectors.Add(v);
                            }

                            _data = vectors;
                            _dataType = typeof(Vector4);
                        }

                        break;
                    }

                case "float4x4":
                    {
                        float[] data = XmlUtil.ParseFloats(xmlData.InnerText);
                        List<Matrix> matrices = new List<Matrix>(_count);

                        for (int i = 0; i < _count; i++)
                        {
                            Matrix M = new Matrix(data[_stride * i + 0], data[_stride * i + 4],
                                                  data[_stride * i + 8], data[_stride * i + 12],
                                                  data[_stride * i + 1], data[_stride * i + 5],
                                                  data[_stride * i + 9], data[_stride * i + 13],
                                                  data[_stride * i + 2], data[_stride * i + 6],
                                                  data[_stride * i + 10], data[_stride * i + 14],
                                                  data[_stride * i + 3], data[_stride * i + 7],
                                                  data[_stride * i + 11], data[_stride * i + 15]);

                            matrices.Add(M);
                        }

                        _data = matrices;
                        _dataType = typeof(Matrix);

                        break;
                    }

                case "name":
                    _data = new List<string>(XmlUtil.ParseNames(xmlData.InnerText));
                    _dataType = typeof(string);
                    break;

                case "idref":
                    _data = new List<string>(XmlUtil.ParseNames(xmlData.InnerText));
                    _dataType = typeof(string);
                    break;

                case "sidref":
                    _data = new List<string>(XmlUtil.ParseNames(xmlData.InnerText));
                    _dataType = typeof(string);
                    break;

                case "int":
                    _data = new List<int>(XmlUtil.ParseInts(xmlData.InnerText));
                    _dataType = typeof(int);
                    break;

                default:
                    throw new NotImplementedException("No implementation for this kind of source");
            }
        }

        /// <summary>
        /// Returns the data of this source as given type, if it actually has a 
        /// compatible type. Otherwise a class cast exception is thrown.
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <returns>List of data</returns>
        public List<T> GetData<T>()
        {
            return (List<T>)_data;
        }

        /// <summary>
        /// Creates a Source instance from the source referenced by given input XML node.
        /// The search for the source node is started from given XML parent.
        /// </summary>
        /// <param name="xmlInput">XML input node</param>
        /// <param name="xmlParent">XML node where to start searching for source node</param>
        /// <returns>Source or null</returns>
        public static ColladaSource FromInput(XmlNode xmlInput, XmlNode xmlParent)
        {
            if (xmlInput.Attributes["source"] != null)
            {
                string sourceId = xmlInput.Attributes["source"].Value.Substring(1);
                XmlNode xmlSource = xmlParent.SelectSingleNode(".//source[@id='" + sourceId + "']");
                Debug.Assert(xmlSource != null, "Source '" + sourceId + "' not found");
                return new ColladaSource(xmlSource);
            }

            return null;
        }
    }
}