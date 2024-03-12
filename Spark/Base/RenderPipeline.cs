namespace Spark
{
    public abstract class RenderPipeline
    {
        public abstract void Initialize(int width, int height);

        public abstract void Resize(int width, int height);

        public abstract void Clear(Camera camera);

        public abstract void Render(Camera camera);

        protected void DrawPass(RenderPass pass)
        {
            CommandBuffer.Execute(pass);
        }
    }
}