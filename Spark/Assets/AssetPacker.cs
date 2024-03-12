using System;
using System.ComponentModel;
using System.IO;

namespace Spark
{
    public abstract class AssetPacker<T> where T : IAsset
    {
        public abstract void Pack(BinaryWriter writer, T value);

        public abstract T Unpack(BinaryReader reader);
    }
}