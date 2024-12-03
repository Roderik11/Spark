using System.Collections.Generic;
using SharpDX;

namespace Spark
{
    [ExecuteInEditor]
    public class TerrainDecorator : Component, IDraw, IUpdate
    {
        private Mesh GenerateMesh(float size)
        {
            // generate cloud of points
            int count = CountPerCell;
            var verts = new Vector3[count];
            var sizes = new Vector3[count];
            var colors = new Vector4[count];

            for (int i = 0; i < count; i++)
            {
                var colorRnd = RNG.RangeFloat(RandomColor.X, RandomColor.Y);
                var rnd = RNG.RangeFloat(RandomSize.X, RandomSize.Y);
                var width = BaseSize.X * rnd;
                var height = BaseSize.Y * rnd;

                var uv = RNG.RangeInt(0, 3);
                colors[i] = Color4.White * colorRnd;
                sizes[i] = new Vector3(width, height, uv);

                float x = RNG.RangeFloat(-size, size);
                float z = RNG.RangeFloat(-size, size);
                verts[i] = new Vector3(x, 0, z);
            }

            Vector3 min = new Vector3(-size, -size, -size);
            Vector3 max = new Vector3(size, size, size);

            var mesh = new Mesh
            {
                Name = "terrainDecoratorCell",
                Vertices = verts,
                Normals = sizes,
                Colors = colors,
                Topology = SharpDX.Direct3D.PrimitiveTopology.PointList,
                BoundingBox = new BoundingBox(min, max)
            };

            mesh.MeshParts.Add(new MeshPart
            {
                Name = "terrainDecoratorCell",
                NumIndices = verts.Length
            });

            return mesh;
        }

        public class Cell
        {
            public long Hash;
            public bool IsOutOfRange;

            private Mesh mesh;
            private BoundingBox bounds;
          
            private readonly MaterialBlock materialBlock = new MaterialBlock();
            private readonly Vector3[] points = new Vector3[8];

            public void Generate(Mesh mesh, Vector3 position)
            {
                this.mesh = mesh;
                var matrix = Matrix.Scaling(Vector3.One) * Matrix.Translation(position);
                materialBlock.SetParameter("World", matrix);

                mesh.BoundingBox.GetCorners(points, matrix);
                bounds = BoundingBox.FromPoints(points);
            }

            public void SetId(int x, int y)
            {
                Hash = PerfectlyHashThem(x, y);
            }

            public void Draw(Material material, ref BoundingFrustum cameraFrustum)
            {
                cameraFrustum.Contains(ref bounds, out var type);
                if(type != ContainmentType.Disjoint)
                    mesh.Render(material, materialBlock);
            }
        }

        public static long PerfectlyHashThem(int a, int b)
        {
            var A = (ulong)(a >= 0 ? 2 * (long)a : -2 * (long)a - 1);
            var B = (ulong)(b >= 0 ? 2 * (long)b : -2 * (long)b - 1);
            var C = (long)((A >= B ? A * A + A + B : A + B * B) / 2);
            return a < 0 && b < 0 || a >= 0 && b >= 0 ? C : -C - 1;
        }


        public int CountPerCell = 10000;
        public float Range = 128;
        public float CellSize = 16;

        public Vector2 BaseSize = new Vector2(0.2f, 0.4f);
        public Vector2 RandomSize = new Vector2(3, 4);
        public Vector2 RandomColor = new Vector2(.8f, 1f);

        public Material Material;
        public Terrain Terrain;

        private Dictionary<long, Cell> patches = new Dictionary<long, Cell>();
        private Pool<Cell> pool = new Pool<Cell>();
        private List<Cell> cells = new List<Cell>();
        private Mesh cellMesh;

        private int lastCellX;
        private int lastCellZ;

        protected override void Awake()
        {
            base.Awake();

            cellMesh = GenerateMesh(CellSize);
        }

        public void Update()
        {
            if (!Enabled) return;

            foreach (var cell in cells)
                cell.IsOutOfRange = true;

            var cameraLocal = Vector3.TransformCoordinate(Camera.Main.WorldPosition, Matrix.Invert(Transform.Matrix));
            cameraLocal *= .5f;

            int cellsPerEdge = (int)(Range / CellSize) / 2;
            int cellx = (int)(cameraLocal.X / CellSize);
            int cellz = (int)(cameraLocal.Z / CellSize);
            int posx, posz;

            if (lastCellX == cellx && lastCellZ == cellz)
                return;

            lastCellX = cellx; lastCellZ = cellz;

            for (int z = -cellsPerEdge; z < cellsPerEdge; z++)
            {
                posz = cellz + z;

                for (int x = -cellsPerEdge; x < cellsPerEdge; x++)
                {
                    posx = cellx + x;

                    long hash = PerfectlyHashThem(posx, posz);
                    if (patches.TryGetValue(hash, out var cell))
                    {
                        cell.IsOutOfRange = false;
                        continue;
                    }

                    var pos = new Vector3(posx * CellSize, 0, posz * CellSize);
                    pos = pos * 2 + Terrain.Transform.Position;
                    pos.Y = Terrain.GetHeight(pos);

                    cell = pool.Get();
                    cell.IsOutOfRange = false;
                    cell.Generate(cellMesh, pos + Transform.Position);
                    cell.SetId(posx, posz);
                    patches.Add(hash, cell);
                    cells.Add(cell);
                }
            }

            for (int i = cells.Count - 1; i >= 0; i--)
            {
                if(cells[i].IsOutOfRange)
                {
                    patches.Remove(cells[i].Hash);
                    cells[i].IsOutOfRange = false;
                    pool.Release(cells[i]);
                    cells.RemoveAt(i);
                }
            }
        }

        public void Draw()
        {
            if (!Enabled) return;

            var effect = Material.Effect;
            effect.SetParameter("ViewProjection", Camera.MainCamera.View * Camera.MainCamera.Projection);
            effect.SetParameter("CameraPosition", Camera.Main.WorldPosition);
            effect.SetParameter("MaxHeight", Terrain.MaxHeight);
            effect.SetParameter("Offset", -Terrain.Transform.WorldPosition);
            effect.SetParameter("MapSize", Terrain.TerrainSize);
            effect.SetParameter("HeightTexel", 1f / Terrain.Heightmap.Description.Width);
            effect.SetParameter("ControlMaps", Terrain.ControlMaps);

            var view = Camera.MainCamera.View * Camera.MainCamera.Projection;
            var frustum = new BoundingFrustum(view);

            cells.For(e =>
            {
                e.Draw(Material, ref frustum);
            });
        }
    }
}