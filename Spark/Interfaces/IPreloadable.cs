namespace Spark
{
    public interface IPreloadable
    {
        string Filename { get; set; }
        int Order { get; }

        void Load(params string[] parameters);
    }
}