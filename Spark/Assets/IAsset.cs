using System;

namespace Spark
{
    public interface IAsset
    {
        Guid Id { get; }
        string Name { get; }
        string Path { get; }
    }

    public abstract class Asset : IAsset
    {
        public Guid Id { get; }

        public string Name { get; set; } = "- undefined -";

        public string Path { get; internal set; }
    }

    public abstract class ScriptableObject : Asset
    {

    }

    public interface IInstance
    {
        int GetInstanceID();
    }
}