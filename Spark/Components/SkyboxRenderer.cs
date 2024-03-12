using System.Collections.Generic;

namespace Spark
{
    [ExecuteInEditor]
    public class SkyBoxRenderer : Component, IDraw, IUpdate
    {
        public Mesh Mesh;
        public List<Material> Materials;
        private MaterialBlock Params;

        public SkyBoxRenderer()
        {
            Materials = new List<Material>();
            Params = new MaterialBlock();
        }

        public void Update()
        {
            if (Camera.Main == null) return;
            Transform.Position = Camera.Main.Position;
        }

        public void Draw()
        {
            if (Mesh == null) return;
            Params.SetParameter("World", Mesh.RootRotation * Transform.Matrix);
            Mesh.Render(Materials, Params);

            //if (Camera.Main == null) return;
            //Transform.Position = Camera.Main.Position;
        }
    }
}