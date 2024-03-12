using System;
using System.ComponentModel;

namespace Spark
{
    public class AssetImporterAttribute : Attribute
    {
        public string[] Formats;

        public AssetImporterAttribute(params string[] formats)
        {
            Formats = formats;
        }
    }

    [Serializable]
    public abstract class AssetReader
    {
        [Browsable(false)]
        public string Filepath;
    }

    [Serializable]
    public class AssetReader<T> : AssetReader where T : IAsset
    {
        public virtual T Import(string filename)
        {
            return default;
        }
    }
}