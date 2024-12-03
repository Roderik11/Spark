using System;
using System.ComponentModel;
using System.IO;

namespace Spark
{
    public class AssetReaderAttribute : Attribute
    {
        public string[] Formats;

        public AssetReaderAttribute(params string[] formats)
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

    public class AssetImporterAttribute : Attribute
    {
        public string[] Formats;

        public AssetImporterAttribute(params string[] formats)
        {
            Formats = formats;
        }
    }

    [Serializable]
    public abstract class AssetImporter
    {
        [Browsable(false)]
        public string Filepath;
    }

    [Serializable]
    public class AssetImporter<T> : AssetImporter where T : IAsset
    {
        public virtual void Import(FileInfo file, DirectoryInfo destination)
        {
        }
    }
}