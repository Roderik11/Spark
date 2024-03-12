using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark
{
    public class StaticMesh : Asset, IStaticMesh
    {
        public Mesh Mesh { get; set; }
        public List<MeshElement> MeshParts { get; set; }

        public LODGroup LODGroup = LODGroups.Trees;
        public List<StaticMeshLOD> LODs = new List<StaticMeshLOD>();

        public StaticMesh()
        {
            MeshParts = new List<MeshElement>();
        }
    }

    [Serializable]
    public class StaticMeshLOD : IStaticMesh
    {
        public Mesh Mesh { get; set; }

        public List<MeshElement> MeshParts { get; set; }

        public StaticMeshLOD()
        {
            MeshParts = new List<MeshElement>();
        }
    }

    public interface IStaticMesh
    {
        Mesh Mesh { get; }
        List<MeshElement> MeshParts { get; }
    }

    [Serializable]
    public class MeshElement
    {
        [Browsable(false)]
        public Mesh Mesh;
        public Material Material = Engine.DefaultMaterial;

        [ValueSelect(typeof(MeshPartProvider))]
        public int MeshPart;
    }
}
