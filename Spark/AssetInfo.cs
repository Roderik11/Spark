using System;
using System.ComponentModel;
using System.IO;

namespace Spark
{
    public class AssetInfo
    {
        public readonly FileInfo FileInfo;
        public readonly Type AssetType;

        [Browsable(true)]
        public string Type => AssetType.Name;

        [Browsable(true)]
        public string FileSize => FileInfo.Length.GetBytesReadable();

        [Browsable(false)]
        public string FullPath => FileInfo.FullName;

        [Browsable(true)]
        public string Name => FileInfo.Name;

        public AssetInfo(string filepath, Type type)
        {
            FileInfo = new FileInfo(filepath);
            AssetType = type;
        }
    }
}
