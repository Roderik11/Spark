using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Systems
{
    public class MeshInstance : Component
    {
        public Mesh Mesh;
        public List<Material> Materials;
        public MaterialBlock MaterialBlock;
    }

    public class MeshRenderSystem : EntitySystem
    {

        public override void Initialize()
        {
        }

        public override void Update()
        {
            var list = new List<MeshInstance>();

            foreach (var item in list)
                item.Mesh.Render(item.Materials, item.MaterialBlock);
        }
    }
}
