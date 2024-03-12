using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using Spark.Noise;
using Spark.Noise.Generator;

namespace Spark
{
    public class Terrain2 : Component, IDraw, IUpdate
    {
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

        private Noise2D Noise;
        private Mesh patch;
        private Frustum frustum;
        private Vector3 view;
        private int patchSize = 64;
        private float sizeFactor;
        private QuadTreeNode quadtree;
        private Dictionary<PatchType, Mesh> patches = new Dictionary<PatchType, Mesh>();
        private List<QuadTreeNode> nodesToDraw = new List<QuadTreeNode>();

        private ComputeShader computeShader;
        private ConstantBufferBox constantBuffer;

        public float MaxHeight = 1024;
        public int MapSize = 4096 * 8;
        public Material Material;
        public Texture Heightmap;
        public float[,] HeightField { get; set; }

        #region gpu noise compute shader source

        private const string computeShaderSource = @"
            // A direct port of Ken Perlin's Java reference to HLSL
            // with the gradients changed to be an array lookup instead of calculating
            // and permutation table NOT duplicated (all lookups perform a '% 256' first)

            // permutation table: 0-255 randomly arranged
            static const int p[] = { 151,160,137,91,90,15,
            131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
            190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
            88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
            77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
            102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
            135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
            5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
            223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
            129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
            251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
            49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
            138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
            };

            // gradients for 3d noise
            static const float3 g[] = {
                1,1,0,
                -1,1,0,
                1,-1,0,
                -1,-1,0,
                1,0,1,
                -1,0,1,
                1,0,-1,
                -1,0,-1,
                0,1,1,
                0,-1,1,
                0,1,-1,
                0,-1,-1,
                1,1,0,
                0,-1,1,
                -1,1,0,
                0,-1,-1,
            };

            float2 fade(float2 t) { return t * t * t * (t * (t * 6 - 15) + 10); }
            float3 fade(float3 t) { return t * t * t * (t * (t * 6 - 15) + 10); }

            float grad(float x, float2 pos)
            {
	            return dot(g[p[x % 256] % 16], pos);
            }

            float grad(float x, float3 pos)
            {
	            return dot(g[p[x % 256] % 16], pos);
            }

            float noise(float2 pos)
            {
	            int2 I = int2(floor(pos)) & 255;

	            pos = frac(pos);

	            float2 fades = fade(pos);

	            int A  = p[I.x      % 256] + I.y;
	            int AA = p[A        % 256];
	            int AB = p[(A + 1)  % 256];
	            int	B  = p[(I.x + 1)% 256] + I.y;
	            int BA = p[B        % 256];
	            int BB = p[(B + 1)  % 256];

	            return lerp(lerp(grad(AA, pos),
	                             grad(BA, pos + float2(-1,  0)), fades.x),
				            lerp(grad(AB, pos + float2( 0, -1)),
				                 grad(BB, pos + float2(-1, -1)), fades.x), fades.y);
            }

            float noise(float3 pos)
            {
	            int3 I = int3(floor(pos)) & 255;

	            pos = frac(pos);

	            float3 fades = fade(pos);

	            int A  = p[I.x     % 256] + I.y;
	            int AA = p[A       % 256] + I.z;
	            int AB = p[(A + 1) % 256] + I.z;
	            int B  = p[(I.x+1) % 256] + I.y;
	            int BA = p[B       % 256] + I.z;
	            int BB = p[(B + 1) % 256] + I.z;

	            return lerp(lerp(lerp(grad(AA,   pos),
						              grad(BA,   pos + float3(-1,  0,  0)), fades.x),
				                 lerp(grad(AB,   pos + float3( 0, -1,  0)),
						              grad(BB,   pos + float3(-1, -1,  0)), fades.x), fades.y),
				            lerp(lerp(grad(AA+1, pos + float3( 0,  0, -1)),
				   		              grad(BA+1, pos + float3(-1,  0, -1)), fades.x),
					             lerp(grad(AB+1, pos + float3( 0, -1, -1)),
						              grad(BB+1, pos + float3(-1, -1, -1)), fades.x), fades.y), fades.z);
            }

            float fBm(float2 pos, int octaves)
            {
	            float lacunarity = 2.0;
	            float gain = 0.5;

	            float freq = 1.0;
	            float amp = 0.5;
	            float sum = 0;
	            for (int i = 0; i < octaves; i++)
	            {
		            sum += noise(pos * freq) * amp;
		            freq *= lacunarity;
		            amp *= gain;
	            }
	            return sum;
            }

            float fBm(float3 pos, int octaves)
            {
	            float lacunarity = 2.0;
	            float gain = 0.5;

	            float freq = 0.25;
	            float amp = 0.5;
	            float sum = 0;
	            for (int i = 0; i < octaves; i++)
	            {
		            sum += noise(pos * freq) * amp;
		            freq *= lacunarity;
		            amp *= gain;
	            }
	            return sum;
            }

            cbuffer params
            {
                float Factor;
                float3 Offset;
            }

            RWTexture2D<float> output;

            [numthreads(32, 32, 1)]
            void NoiseMap(uint3 groupID : SV_GroupID, uint3 threadID : SV_DispatchThreadID)
            {
                //float n = noise((Offset * 100 + float3(threadID.x, 0, 64 - threadID.y) / 64.0 * Factor) * .25f);
                float n = fBm(Offset * 100 + float3(threadID.x, 0, 64 - threadID.y) / 63.0 * Factor, 8);
                output[threadID.xy] = n * 1;;
            }

            ";

        #endregion gpu noise compute shader source

        protected void Awake()
        {
            float extent = MapSize / 2;
            //quadtree = new QuadTreeNode(Transform.Position, new Vector3(extent, 100000, extent));
            quadtree = new QuadTreeNode(Transform.Position + new Vector3(extent, 0, extent), new Vector3(extent, MaxHeight * 100, extent));
            //patch = Mesh.CreatePatch();
            sizeFactor = quadtree.Extents.X * 2 / patchSize;

            patches.Add(PatchType.Default, Mesh.CreatePatch());
            patches.Add(PatchType.N, Mesh.PatchN());
            patches.Add(PatchType.E, Mesh.PatchE());
            patches.Add(PatchType.S, Mesh.PatchS());
            patches.Add(PatchType.W, Mesh.PatchW());
            patches.Add(PatchType.NW, Mesh.PatchNW());
            patches.Add(PatchType.NE, Mesh.PatchNE());
            patches.Add(PatchType.SW, Mesh.PatchSW());
            patches.Add(PatchType.SE, Mesh.PatchSE());

            Noise = new Noise2D(patchSize + 1, patchSize + 1);

            //Noise.Generator = new Generator.Billow() { Frequency = 6, OctaveCount = 12, Seed = 687266 };
            //Noise.Generator = new Generator.Perlin() { Frequency = 6, OctaveCount = 12, Seed = 687266 };
            Noise.Generator = new RiggedMultifractal() { Frequency = 4, OctaveCount = 12, Seed = 687266, Lacunarity = 1.89786 };

            CreatePerlinGPU();
        }

        private void CreatePerlinGPU()
        {
            using (ShaderBytecode integrateBytecode = ShaderBytecode.Compile(computeShaderSource, "NoiseMap", "cs_5_0", ShaderFlags.None, EffectFlags.None, ""))
            {
                computeShader = new ComputeShader(Engine.Device, integrateBytecode);
                ShaderReflection reflection = new ShaderReflection(integrateBytecode);
                constantBuffer = new ConstantBufferBox(reflection.GetConstantBuffer(0));
            }
        }

        public void Update()
        {
            view = Camera.Main.WorldPosition;
            //view.Y = 0;
            quadtree.Update(view);
        }

        public float GetHeight(Vector3 position)
        {
            int x = (int)(position.X - Transform.Position.X);
            int z = (int)(position.Z - Transform.Position.Z);

            float xf = position.X - x;
            float zf = position.Z - z;

            float h1 = HeightField[x, z];
            float h2 = HeightField[x + 1, z];

            float h3 = HeightField[x, z + 1];
            float h4 = HeightField[x + 1, z + 1];

            float height = 0.0f;

            if (xf + zf < 1)
            {
                height = h1;
                height += (h2 - h1) * xf;
                height += (h3 - h1) * zf;
            }
            else
            {
                height = h4;
                height += (h2 - h4) * (1.0f - zf);
                height += (h3 - h4) * (1.0f - xf);
            }

            return height;
        }

        public void Draw()
        {
            frustum = new Frustum(Camera.MainCamera.View * Camera.MainCamera.Projection);
            quadtree.Render(AddPatch);

            nodesToDraw.Sort((a, b) => b.Depth.CompareTo(a.Depth));

            foreach (QuadTreeNode node in nodesToDraw)
            {
                PrepareNode(node);
                CreateHeightmap(node);
                RenderPatch(node);
            }

            nodesToDraw.Clear();

            // Pipeline.Enqueue(RenderPass.Debug, quadtree.Draw);
        }

        private void CreateHeightmap(QuadTreeNode node)
        {
            NodePayload payload = node.Payload as NodePayload;
            if (payload.Heightmap != null) return;

            Texture2D res = new Texture2D(Engine.Device, new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = SharpDX.DXGI.Format.R32_Float,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Height = 64,
                Width = 64,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default
            });

            Texture result = new Texture
            {
                Resource = res,
                View = new ShaderResourceView(Engine.Device, res)
            };

            double t = 100d / Math.Pow(2, node.Depth);

            double x = (node.Bounds.Minimum.X + quadtree.Extents.X) / (quadtree.Extents.X * 2);
            double y = (node.Bounds.Maximum.Z + quadtree.Extents.Z) / (quadtree.Extents.Z * 2);
            double z = (node.Bounds.Maximum.X + quadtree.Extents.X) / (quadtree.Extents.X * 2);
            double w = (node.Bounds.Minimum.Z + quadtree.Extents.Z) / (quadtree.Extents.Z * 2);

            UnorderedAccessView uav = new UnorderedAccessView(Engine.Device, result.Resource);

            constantBuffer.SetParameter("Factor", (float)t);
            constantBuffer.SetParameter("Offset", new Vector3((float)x, 0, (float)(1 - y)));
            constantBuffer.Commit();

            Engine.Device.ImmediateContext.ComputeShader.Set(computeShader);
            Engine.Device.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, uav);
            Engine.Device.ImmediateContext.ComputeShader.SetConstantBuffer(0, constantBuffer.Buffer);
            Engine.Device.ImmediateContext.Dispatch(2, 2, 1);

            payload.Heightmap = result;

            Engine.Device.ImmediateContext.ComputeShader.Set(null);
            Engine.Device.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, null);
            Engine.Device.ImmediateContext.ComputeShader.SetConstantBuffer(0, null);

            //Vector3 min = node.Bounds.Minimum;
            //Vector3 max = node.Bounds.Maximum;

            //min.Y = Noise.Minimum * MaxHeight;
            //max.Y = Noise.Maximum * MaxHeight;

            //node.Bounds = new SharpDX.BoundingBox(min, max);
        }

        private void CreateHeightmap2(QuadTreeNode node)
        {
            NodePayload payload = node.Payload as NodePayload;
            if (payload.Heightmap != null) return;

            double t = 1d / (patchSize * Math.Pow(2, node.Depth)) * 1;

            double x = (node.Bounds.Minimum.X + quadtree.Extents.X) / (quadtree.Extents.X * 2);
            double y = (node.Bounds.Maximum.Z + quadtree.Extents.Z) / (quadtree.Extents.Z * 2);
            double z = (node.Bounds.Maximum.X + quadtree.Extents.X) / (quadtree.Extents.X * 2);
            double w = (node.Bounds.Minimum.Z + quadtree.Extents.Z) / (quadtree.Extents.Z * 2);

            Noise.GeneratePlanar(x, z + t, w, y + t, false);
            payload.Heightmap = Noise.Get32BitTexture();

            Vector3 min = node.Bounds.Minimum;
            Vector3 max = node.Bounds.Maximum;

            min.Y = Noise.Minimum * MaxHeight;
            max.Y = Noise.Maximum * MaxHeight;

            node.Bounds = new SharpDX.BoundingBox(min, max);
        }

        private void PrepareNode(QuadTreeNode node)
        {
            if (node.Payload == null) node.Payload = new NodePayload();
            NodePayload payload = node.Payload as NodePayload;
            payload.Type = GetPatchType(node);
        }

        private void AddPatch(QuadTreeNode node)
        {
            if (frustum.Intersects(node.Bounds) == ContainmentType.Disjoint) return;
            nodesToDraw.Add(node);
        }

        private void RenderPatch(QuadTreeNode node)
        {
            NodePayload payload = node.Payload as NodePayload;
            MaterialBlock materialBlock = payload.Block;
            payload.Type = GetPatchType(node);

            float s = 2 / (float)Math.Pow(2, node.Depth) * sizeFactor;

            Matrix translation = Matrix.Translation(node.Position - Transform.Position);
            Matrix scale = Matrix.Scaling(s, 1f, s);
            // Matrix scale = Matrix.Scaling(s, 1f / (float)Math.Pow(2, node.Depth), s);
            Matrix m = scale * translation;

            Vector4 rect = Vector4.Zero;
            //rect.X = (node.Bounds.Minimum.X + quadtree.Extents.X) / (quadtree.Extents.X * 2);
            //rect.Y = (node.Bounds.Maximum.Z + quadtree.Extents.Z) / (quadtree.Extents.Z * 2);
            //rect.Z = (node.Bounds.Maximum.X + quadtree.Extents.X) / (quadtree.Extents.X * 2);
            //rect.W = (node.Bounds.Minimum.Z + quadtree.Extents.Z) / (quadtree.Extents.Z * 2);

            rect.X = (node.Bounds.Minimum.X - Transform.Position.X) / (quadtree.Extents.X * 2);
            rect.Y = (node.Bounds.Maximum.Z - Transform.Position.Z) / (quadtree.Extents.Z * 2);
            rect.Z = (node.Bounds.Maximum.X - Transform.Position.X) / (quadtree.Extents.X * 2);
            rect.W = (node.Bounds.Minimum.Z - Transform.Position.Z) / (quadtree.Extents.Z * 2);

            materialBlock.SetParameter("Height", payload.Heightmap);
            //materialBlock.SetParameter("Height", Heightmap);
            materialBlock.SetParameter("sampHeightFilter", Samplers.ClampedBilinear2D);
            materialBlock.SetParameter("sampHeight", Samplers.ClampedPoint2D);
            materialBlock.SetParameter("sampData", Samplers.WrappedAnisotropic);
            materialBlock.SetParameter("World", m * Transform.Matrix);
            materialBlock.SetParameter("Rect", rect);
            materialBlock.SetParameter("Level", node.Depth + 1);
            materialBlock.SetParameter("MapSize", patchSize);
            materialBlock.SetParameter("MaxHeight", MaxHeight);

            materialBlock.SetParameter("Tiling", Vector2.One * 32 * (float)Math.Pow(2, QuadTreeNode.MaxDepth - node.Depth));
            //materialBlock.SetParameter("Tiling", Vector2.One * 16);
            //effect.SetParameter("Tiling", Vector2.One);

            patch = patches[payload.Type];
            patch.Render(Material, materialBlock);
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