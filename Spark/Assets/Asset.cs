using System;

namespace Spark
{
    public abstract class Asset : IAsset
    {
        public Guid Id { get; }

        public string Name { get; set; } = "- undefined -";

        public string Path { get; internal set; }
    }

    public abstract class ScriptableObject : Asset
    {

    }

}