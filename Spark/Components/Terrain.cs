using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Spark
{
    [ExecuteInEditor]
    public class Terrain : Component, IDraw, IUpdate
    {
        [Serializable]
        public class TextureLayer
        {
            public Texture Diffuse;
            public Texture Normals;
            public Vector2 Tiling = Vector2.One;
        }

        public struct TerrainPatch
        {
            public Matrix Transform;
            public Vector4 Rect;
            public Vector2 Level;
        }

        struct DrawArgs
        {
            public uint IndexCountPerInstance;
            public uint InstanceCount;
            public uint StartIndexLocation;
            public uint BaseVertexLocation;
            public uint StartInstanceLocation;
        }

        public enum PatchType
        {
            Default,
            N, E, S, W,
            NE, NW, SE, SW
        }

        public class NodePayload
        {
            public MaterialBlock Block = new MaterialBlock();
            public Texture Heightmap;
            public PatchType Type;
        }

        class Bucket
        {
            public int drawCount;
            public Mesh mesh;
            public ShaderResourceView instanceBufferView;

            private Buffer buffer;
            private DataStream stream;
            private TerrainPatch[] array;
            //private StructuredBuffer<DrawArgs> argsBuffer;

            public Bucket(Mesh mesh)
            {
                this.mesh = mesh;

                int stride = Utilities.SizeOf<TerrainPatch>();
                array = new TerrainPatch[32];

                buffer = new Buffer(Engine.Device, new BufferDescription()
                {
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = ResourceOptionFlags.BufferStructured,
                    SizeInBytes = stride * array.Length,
                    Usage = ResourceUsage.Dynamic,
                    StructureByteStride = stride
                });

                instanceBufferView = new ShaderResourceView(Engine.Device, buffer);
                
                //argsBuffer = new StructuredBuffer<DrawArgs>(Engine.Device.ImmediateContext, ResourceOptionFlags.DrawIndirectArguments, BindFlags.UnorderedAccess);
                //argsBuffer.Add(new DrawArgs());
            }


            private void ResizeBuffers()
            {
                int stride = Utilities.SizeOf<TerrainPatch>();

                Array.Resize(ref array, array.Length * 2);

                Disposer.SafeDispose(ref buffer);
                Disposer.SafeDispose(ref instanceBufferView);

                buffer = new Buffer(Engine.Device, new BufferDescription()
                {
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = ResourceOptionFlags.BufferStructured,
                    SizeInBytes = stride * array.Length,
                    Usage = ResourceUsage.Dynamic,
                    StructureByteStride = stride
                });

                instanceBufferView = new ShaderResourceView(Engine.Device, buffer);
            }

            public void Add(Matrix matrix, Vector4 rect, int level)
            {
                array[drawCount] = new TerrainPatch
                {
                    Transform = Matrix.Transpose(matrix),
                    Rect = rect,
                    Level = new Vector2(level, 0)
                };

                drawCount++;

                if (drawCount >= array.Length)
                    ResizeBuffers();
            }

            public void Clear()
            {
                drawCount = 0;
            }

            public void WriteInstanceBuffer()
            {
                if (drawCount == 0) return;
                Engine.Device.ImmediateContext.MapSubresource(buffer, 0, MapMode.WriteDiscard, MapFlags.None, out stream);
                stream.WriteRange(array, 0, drawCount);
                Engine.Device.ImmediateContext.UnmapSubresource(buffer, 0);
            }

            public void Draw(Effect effect)
            {
                if (drawCount == 0) return;

                var assembler = Engine.Device.ImmediateContext.InputAssembler;

                assembler.SetVertexBuffers(0, mesh.Bindings);
                assembler.SetIndexBuffer(mesh.IndexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);

                effect.SetParameter("terrainData", instanceBufferView);
                effect.Apply();

                CommandBuffer.IncrementDrawInstanced(RenderPass.Opaque, 1);
                CommandBuffer.IncrementInstances(RenderPass.Opaque, drawCount);

                foreach (MeshPart part in mesh.MeshParts)
                {
                    //argsBuffer[0] = new DrawArgs
                    //{
                    //    IndexCountPerInstance = (uint)part.NumIndices,
                    //    InstanceCount = (uint)drawCount,
                    //    StartIndexLocation = (uint)part.BaseIndex,
                    //    BaseVertexLocation = (uint)part.BaseVertex,
                    //    StartInstanceLocation = 0,
                    //};

                    Engine.Device.ImmediateContext.DrawIndexedInstanced(part.NumIndices, drawCount, part.BaseIndex, part.BaseVertex, 0);
                    //Engine.Device.ImmediateContext.DrawIndexedInstancedIndirect(argsBuffer.Buffer, 0);
                }
            }
        }

        private Frustum frustum;
        private float sizeFactor;

        private readonly int patchSize = 32;
        private QuadTreeNode quadtree;
        private readonly Dictionary<PatchType, Mesh> patches = new Dictionary<PatchType, Mesh>();
        private readonly List<QuadTreeNode> nodesToRender = new List<QuadTreeNode>();
        private readonly Dictionary<PatchType, Bucket> buckets = new Dictionary<PatchType, Bucket>();
        private Effect createNormalsEffect;

        public bool Wireframe;

        public Texture Heightmap;
        public Material Material;

        public Vector2 TerrainSize = new Vector2(4096 * 2, 4096 * 2);
        public float MaxHeight = 1024;

        public List<TextureLayer> Layers = new List<TextureLayer>();

        public float[,] HeightField { get; set; }
        //public RenderTexture2D NormalMap { get; private set; }

        public Texture ControlMaps;
        private Texture DiffuseMaps;
        private Texture NormalMaps;
        private Vector2[] LayerTiling = new Vector2[32];

        protected override void Awake()
        {
            var extent = TerrainSize / 2;
            quadtree = new QuadTreeNode(Transform.Position + new Vector3(extent.X, 0, extent.Y), new Vector3(extent.X, MaxHeight * 10, extent.Y));
            sizeFactor = quadtree.Extents.X / patchSize;

            patches.Add(PatchType.Default, Mesh.CreatePatch());
            patches.Add(PatchType.N, Mesh.PatchN());
            patches.Add(PatchType.E, Mesh.PatchE());
            patches.Add(PatchType.S, Mesh.PatchS());
            patches.Add(PatchType.W, Mesh.PatchW());
            patches.Add(PatchType.NW, Mesh.PatchNW());
            patches.Add(PatchType.NE, Mesh.PatchNE());
            patches.Add(PatchType.SW, Mesh.PatchSW());
            patches.Add(PatchType.SE, Mesh.PatchSE());

            foreach (var kp in patches)
            {
                kp.Value.UV = null;
                kp.Value.Normals = null;
            }

            //NormalMap = new RenderTexture2D(Heightmap.Description.Width, Heightmap.Description.Height, SharpDX.DXGI.Format.R16G16B16A16_SNorm, false);

            //createNormalsEffect = new Effect("createNormals");
            //createNormalsEffect.SetParameter("HeightTexel", 1f / Heightmap.Description.Width);
            //createNormalsEffect.SetParameter("MainTexture", Heightmap);
            //createNormalsEffect.SetParameter("sampData", Samplers.ClampedPoint2D);

            //Graphics.SetViewport(0, 0, Heightmap.Description.Width, Heightmap.Description.Height);
            //Graphics.Blit(Heightmap, NormalMap.Target, createNormalsEffect);

            CreateControlMaps();
            CreateTextureArrays();
        }

        private VertexBufferBinding CreateVertexBuffer2()
        {
            int num = 33;
            int count = num * num;
            float offset = (float)(num - 1) / 2;

            var verts = new short[count];

            for (int z = 0; z < num; z++)
            {
                for (int x = 0; x < num; x++)
                {
                    int index = z * num + x;
                    verts[index] = (short)((x & 0x3F) | ((z & 0x3F) << 6));
                }
            }

            var buffer = Geometry.CreateVertexBuffer(verts);

            return new VertexBufferBinding(buffer, buffer.Description.StructureByteStride, 0);
        }

        private void CreateVertexBuffer(out Buffer buffer, out VertexBufferBinding binding)
        {
            int num = 33;
            int count = num * num;
            float offset = (float)(num - 1) / 2;

            var verts = new Vector3[count];

            for (int z = 0; z < num; z++)
            {
                for (int x = 0; x < num; x++)
                {
                    int index = z * num + x;
                    verts[index] = new Vector3(x - offset, 0, z - offset);
                }
            }

            buffer = Geometry.CreateVertexBuffer(verts);
            binding = new VertexBufferBinding(buffer, Utilities.SizeOf<Vector3>(), 0);
        }


        private void CreateControlMaps()
        {
            ControlMaps?.Dispose();

            List<Texture> controlMaps = new List<Texture>
            {
                Engine.Assets.Load<Texture>("Terrain/Terrain_splatmap_0.dds"),
                Engine.Assets.Load<Texture>("Terrain/Terrain_splatmap_1.dds"),
                Engine.Assets.Load<Texture>("Terrain/Terrain_splatmap_2.dds"),
                Engine.Assets.Load<Texture>("Terrain/Terrain_splatmap_3.dds"),
            };

            ControlMaps = Graphics.CreateTexture2DArray(1024, 1024, controlMaps);
            
            Material.Effect.SetValue("ControlMaps", ControlMaps);
        }

        private void CreateTextureArrays()
        {
            DiffuseMaps?.Dispose();
            NormalMaps?.Dispose();
        
            var diffuse = new List<Texture>();
            var normals = new List<Texture>();
            var tiling = new List<Vector2>();

            for (int i = 0; i < Layers.Count; i++)
            {
                var layer = Layers[i];
                diffuse.Add(layer.Diffuse);
                normals.Add(layer.Normals);
                tiling.Add(TerrainSize / layer.Tiling);
            }

            LayerTiling = tiling.ToArray();
            DiffuseMaps = Graphics.CreateTexture2DArray(1024, 1024, diffuse);
            NormalMaps = Graphics.CreateTexture2DArray(1024, 1024, normals);

            Material.Effect.SetValue("LayerTiling", LayerTiling);
            Material.Effect.SetValue("DiffuseMaps", DiffuseMaps);
            Material.Effect.SetValue("NormalMaps", NormalMaps);
        }

        public float GetHeight(Vector3 position)
        {
            position -= Transform.Position;

            var dimx = HeightField.GetLength(0) - 1;
            var dimy = HeightField.GetLength(1) - 1;

            var fx = dimx / TerrainSize.X;
            var fy = dimy / TerrainSize.Y;

            position.X *= fx;
            position.Z *= fy;

            int x = (int)Math.Floor(position.X);
            int z = (int)Math.Floor(position.Z);

            if (x < 0 || x > dimx) return 0;
            if (z < 0 || z > dimy) return 0;

            float xf = position.X - x;
            float zf = position.Z - z;

            float h1 = HeightField[x, z];
            float h2 = HeightField[Math.Min(dimx, x + 1), z];
            float h3 = HeightField[x, Math.Min(dimy, z + 1)];
            
            //float h4 = HeightField[Math.Min(dimx, x + 1), Math.Min(dimy, z + 1)];
            
            float height = h1;
            height += (h2 - h1) * xf;
            height += (h3 - h1) * zf;

            return Transform.Position.Y + height;

            //if (xf + zf < 1)
            //{
            //    height = h1;
            //    height += (h2 - h1) * xf;
            //    height += (h3 - h1) * zf;
            //}
            //else
            //{
            //    height = h4;
            //    height += (h2 - h4) * (1.0f - zf);
            //    height += (h3 - h4) * (1.0f - xf);
            //}

            //return height;
        }

        public void Update()
        {
            Material.Effect.RasterizerState = Wireframe ? States.Wireframe : States.BackCull;

            Profiler.Start("Terrain Quadtree");
            quadtree.Update(Camera.Main.WorldPosition);
            Profiler.Stop();
        }

        public void Draw()
        {
            frustum = new Frustum(Camera.MainCamera.View * Camera.MainCamera.Projection);
            quadtree.Render(CheckPatchVisible);

            nodesToRender.Sort((a, b) => b.Depth.CompareTo(a.Depth));

            foreach (var pair in buckets)
                pair.Value.Clear();

            foreach (var node in nodesToRender)
            {
                PrepareNode(node);
                RenderPatch(node);
            }

            nodesToRender.Clear();

            CommandBuffer.Enqueue(RenderPass.Opaque, DrawInstanced);
            // CommandBuffer.Enqueue(RenderPass.Debug, quadtree.Draw);
        }

        private void PrepareNode(QuadTreeNode node)
        {
            if (node.Payload == null) node.Payload = new NodePayload();
            NodePayload payload = node.Payload as NodePayload;
            payload.Type = GetPatchType(node);
            //payload.Type = PatchType.Default;
        }

        private void CheckPatchVisible(QuadTreeNode node)
        {
            if (frustum.Intersects(node.Bounds) == ContainmentType.Disjoint)
                return;

            nodesToRender.Add(node);
        }

        private void RenderPatch(QuadTreeNode node)
        {
            NodePayload payload = node.Payload as NodePayload;

            float s = 2 / (float)Math.Pow(2, node.Depth) * sizeFactor;
            Matrix translation = Matrix.Translation(node.Position - Transform.Position);
            Matrix scale = Matrix.Scaling(s, 1f, s);
            Matrix m = scale * translation;

            Vector4 rect = Vector4.Zero;
            rect.X = (node.Bounds.Minimum.X - Transform.Position.X) / (quadtree.Extents.X * 2);
            rect.Y = (node.Bounds.Maximum.Z - Transform.Position.Z) / (quadtree.Extents.Z * 2);
            rect.Z = (node.Bounds.Maximum.X - Transform.Position.X) / (quadtree.Extents.X * 2);
            rect.W = (node.Bounds.Minimum.Z - Transform.Position.Z) / (quadtree.Extents.Z * 2);

            if(!buckets.TryGetValue(payload.Type, out var bucket))
            {
                bucket = new Bucket(patches[payload.Type]);
                buckets.Add(payload.Type, bucket);
            }

            bucket.Add(m * Transform.Matrix, rect, QuadTreeNode.MaxDepth - (node.Depth + 1));
        }      

        void DrawInstanced()
        {
            Profiler.Start("Terrain Draw Instanced");

            var campos = Camera.Main.WorldPosition;
            campos.Y = 0;

            var effect = Material.Effect;
            effect.SetParameter("sampData", Samplers.WrappedAnisotropic);
            effect.SetParameter("sampHeight", Samplers.ClampedPoint2D);
            effect.SetParameter("sampHeightFilter", Samplers.ClampedBilinear2D);
            effect.SetParameter("Height", Heightmap);
            effect.SetParameter("MaxHeight", MaxHeight);
            effect.SetParameter("HeightTexel", 1f / Heightmap.Description.Width);
            effect.SetParameter("CameraPos", campos);

            foreach (var pair in buckets)
                pair.Value.WriteInstanceBuffer();

            var assembler = Engine.Device.ImmediateContext.InputAssembler;
            assembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            assembler.InputLayout = effect.GetInputLayout(VertexPosition.InputElements);

            foreach (var pair in buckets)
                pair.Value.Draw(effect);

            Profiler.Stop();
        }

        private PatchType GetPatchType(QuadTreeNode node)
        {
            if (node.Location == 0) // SW
            {
                var south = node.Neighbors[0];
                var west = node.Neighbors[3];

                bool left = west != null && west.Depth < node.Depth;
                bool bottom = south != null && south.Depth < node.Depth;

                if (left && bottom) return PatchType.SW;
                if (left) return PatchType.W;
                if (bottom) return PatchType.S;
            }
            else if (node.Location == 1) // SE
            {
                var south = node.Neighbors[0];
                var east = node.Neighbors[1];

                bool right = east != null && east.Depth == node.Depth - 1;
                bool bottom = south != null && south.Depth == node.Depth - 1;

                if (right && bottom) return PatchType.SE;
                if (right) return PatchType.E;
                if (bottom) return PatchType.S;
            }
            else if (node.Location == 2) // NW
            {
                var north = node.Neighbors[2];
                var west = node.Neighbors[3];

                bool left = west != null && west.Depth < node.Depth;
                bool top = north != null && north.Depth < node.Depth;

                if (left && top) return PatchType.NW;
                if (left) return PatchType.W;
                if (top) return PatchType.N;
            }
            else if (node.Location == 3) // NE
            {
                var north = node.Neighbors[2];
                var east = node.Neighbors[1];

                bool right = east != null && east.Depth == node.Depth - 1;
                bool top = north != null && north.Depth == node.Depth - 1;

                if (right && top) return PatchType.NE;
                if (right) return PatchType.E;
                if (top) return PatchType.N;
            }

            return PatchType.Default;
        }
    }
}