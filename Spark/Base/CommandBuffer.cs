using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Assimp;
using Jitter.Dynamics;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Spark
{
    public class CommandBuffer
    {
        private static readonly List<InstanceBatch> batchlist = new List<InstanceBatch>();
        private static readonly Dictionary<BatchKey, InstanceBatch> batches = new Dictionary<BatchKey, InstanceBatch>();
        private static readonly Dictionary<RenderPass, int> TotalDrawNonInstanced = new Dictionary<RenderPass, int>();
        private static readonly Dictionary<RenderPass, int> TotalDrawInstanced = new Dictionary<RenderPass, int>();
        private static readonly Dictionary<RenderPass, int> TotalInstances = new Dictionary<RenderPass, int>();

        private static readonly Stack<CommandBuffer> stack = new Stack<CommandBuffer>();
        private static CommandBuffer current;

        private readonly Dictionary<RenderPass, Pass> Passes = new Dictionary<RenderPass, Pass>();
        private static readonly Stack<CommandBuffer> freeBuffers = new Stack<CommandBuffer>();

        private static readonly ConcurrentDictionary<BatchKey, InstanceBatch> batchesConc = new ConcurrentDictionary<BatchKey, InstanceBatch>();

        class DrawCallList : ConcurrentStack<DrawCall>
        {

        }

        struct BatchKey : IEquatable<BatchKey>
        {
            public int material;
            public int mesh;
            public int meshpart;

            public bool Equals(BatchKey other)
            {
                return other.material == material && other.mesh == mesh && other.meshpart == meshpart; 
            }
        }

        class BatchKeyComparer : IEqualityComparer<BatchKey>
        {
            public bool Equals(BatchKey x, BatchKey y)
            {
                return x.Equals(y);
            }

            public int GetHashCode(BatchKey obj)
            {
                return obj.GetHashCode();
            }
        }

        class Pass
        {
            public DrawCallList Callbacks = new DrawCallList();
            public DrawCallList DrawCalls = new DrawCallList();
            public DrawCallList InstancedCalls = new DrawCallList();

            public DrawCallList[] Quickset;

            public Pass()
            {
                Quickset = new DrawCallList[2] { DrawCalls, InstancedCalls };
            }

            public void Clear()
            {
                if (InstancedCalls.Count > 0) InstancedCalls.Clear();
                if (DrawCalls.Count > 0) DrawCalls.Clear();
                if (Callbacks.Count > 0) Callbacks.Clear();
            }
        }

        struct DrawArgs
        {
            public uint IndexCountPerInstance;
            public uint InstanceCount;
            public uint StartIndexLocation;
            public uint BaseVertexLocation;
            public uint StartInstanceLocation;
        }


        /// <summary>
        /// Multi-vertex-stream instancing is faster than using Indirect with a StructuredBuffer 
        /// /// </summary>
        private class InstanceBatch
        {
            public int drawCount;
            public Mesh mesh;
            public MeshPart meshPart;
            public Material material;

            private StructuredBuffer<DrawArgs> argsBuffer;
            //private ShaderResourceView instanceBufferView;
            private VertexBufferBinding instanceBufferBinding;

            private Buffer matrixBuffer;
            private DataStream matrixData;
            private Matrix[] matrixArray;

            private DrawCall[] drawCalls;
            private int drawCallArrayLength = 512;

            public float magnitude;
                
            public InstanceBatch(Mesh mesh, int meshPart, Material material)
            {
                this.mesh = mesh;
                this.meshPart = mesh.MeshParts[meshPart];
                this.material = material;

                int stride = Utilities.SizeOf<Matrix>();
                matrixArray = new Matrix[32];
                drawCalls = new DrawCall[drawCallArrayLength];

                magnitude = mesh.BoundingBox.Size.Length();

                matrixBuffer = new Buffer(Engine.Device, new BufferDescription()
                {
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = stride * matrixArray.Length,
                    Usage = ResourceUsage.Dynamic,
                    StructureByteStride = stride
                });

                //instanceBufferView = new ShaderResourceView(Engine.Device, buffer);
                instanceBufferBinding = new VertexBufferBinding(matrixBuffer, stride, 0);

                argsBuffer = new StructuredBuffer<DrawArgs>(Engine.Device.ImmediateContext, ResourceOptionFlags.DrawIndirectArguments, BindFlags.UnorderedAccess);
                argsBuffer.Add(new DrawArgs());
            }

            public void Add(DrawCall drawCall)
            {
                drawCalls[drawCount] = drawCall;
                drawCount++;

                if (drawCount >= drawCallArrayLength)
                {
                    drawCallArrayLength *= 2;
                    Array.Resize(ref drawCalls, drawCallArrayLength);
                }
            }

            private void ResizeMatrixBuffer(int size)
            {
                int stride = Utilities.SizeOf<Matrix>();

                Array.Resize(ref matrixArray, size * 2);

                Disposer.SafeDispose(ref matrixBuffer);
                //Disposer.SafeDispose(ref instanceBufferView);

                matrixBuffer = new Buffer(Engine.Device, new BufferDescription()
                {
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = stride * matrixArray.Length,
                    Usage = ResourceUsage.Dynamic,
                    StructureByteStride = stride
                });

                instanceBufferBinding.Buffer = matrixBuffer;
                //instanceBufferView = new ShaderResourceView(Engine.Device, buffer);
            }

            internal void Prepare(Vector3 origin)
            {
                for (int i = 0; i < drawCount; i++)
                {
                    ref var dc = ref drawCalls[i];
                    var position = dc.Params.GetValue<Matrix>("World").TranslationVector;
                    Vector3.DistanceSquared(ref position, ref origin, out dc.DistanceSqrd);
                }
            }

            internal void FillArrays()
            {
                if (drawCount >= matrixArray.Length)
                    ResizeMatrixBuffer(drawCount);

                for (int i = 0; i < drawCount; i++)
                {
                    var matrix = drawCalls[i].Params.GetValue<Matrix>("World");
                    Matrix.Transpose(ref matrix, out matrixArray[i]);
                }
            }

            internal void SortDrawCalls()
            {
                Array.Sort(drawCalls, 0, drawCount, DrawCallComparer.Instance);
            }

            internal void WriteBuffers()
            {
                if (drawCount == 0) return;

                var context = Engine.Device.ImmediateContext;

                context.MapSubresource(matrixBuffer, 0, MapMode.WriteDiscard, MapFlags.None, out matrixData);
                matrixData.WriteRange(matrixArray, 0, drawCount);
                context.UnmapSubresource(matrixBuffer, 0);
            }

            public void Draw(RenderPass renderPass)
            {
                if (drawCount == 0) return;

                var context = Engine.Device.ImmediateContext;
                var assembler = context.InputAssembler;

                if (material.Technique < 2)
                    material.Technique = material.UseInstancing ? 1 : 0;

                material.SetPass(renderPass);
                //effect.SetParameter("matrixBuffer", instanceBufferView);
                material.SetParameter("World", Matrix.Identity);
                material.Apply();

                assembler.PrimitiveTopology = mesh.Topology;
                assembler.InputLayout = material.GetInputLayout(mesh.InputElements);
                assembler.SetIndexBuffer(mesh.IndexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);
                assembler.SetVertexBuffers(0, mesh.Bindings);
                assembler.SetVertexBuffers(9, instanceBufferBinding);

                if (renderPass == RenderPass.ShadowMap)
                    Engine.Device.ImmediateContext.Rasterizer.State = States.FrontCull;

                IncrementDrawInstanced(renderPass, 1);
                IncrementInstances(renderPass, drawCount);

            //    ref var args = ref argsBuffer[0];

                //argsBuffer[0] = new DrawArgs
                //{
                //    IndexCountPerInstance = (uint)meshPart.NumIndices,
                //    InstanceCount = (uint)drawCount,
                //    StartIndexLocation = (uint)meshPart.BaseIndex,
                //    BaseVertexLocation = (uint)meshPart.BaseVertex,
                //    StartInstanceLocation = 0,
                //};

                if (mesh.IndexBuffer != null)
                //    context.DrawIndexedInstancedIndirect(argsBuffer.Buffer, 0);
                    context.DrawIndexedInstanced(meshPart.NumIndices, drawCount, meshPart.BaseIndex, meshPart.BaseVertex, 0);
                else
                    context.DrawInstanced(meshPart.NumIndices, drawCount, meshPart.BaseVertex, 0);
            }

            public void BindAndDraw(RenderPass renderPass)
            {
                if (drawCount == 0) return;

                var context = Engine.Device.ImmediateContext;
                var assembler = context.InputAssembler;

                assembler.SetVertexBuffers(9, instanceBufferBinding);

                IncrementDrawInstanced(renderPass, 1);
                IncrementInstances(renderPass, drawCount);

                if (mesh.IndexBuffer != null)
                    context.DrawIndexedInstanced(meshPart.NumIndices, drawCount, meshPart.BaseIndex, meshPart.BaseVertex, 0);
                else
                    context.DrawInstanced(meshPart.NumIndices, drawCount, meshPart.BaseVertex, 0);
            }

            public void Clear()
            {
                if (drawCount == 0) return;

                drawCount = 0;
            }
        }

        private struct DrawCall
        {
            //public long Hash;
            public BatchKey Key;
            public Mesh Mesh;
            public int MeshPart;
            public Material Material;
            public MaterialBlock Params;
            public Action Callback;
            public float DistanceSqrd;
        }

        private class DrawCallComparer : IComparer<DrawCall>
        {
            public static DrawCallComparer Instance;

            static DrawCallComparer()
            {
                Instance = new DrawCallComparer();
            }

            public int Compare(DrawCall a, DrawCall b)
            {
                return a.DistanceSqrd.CompareTo(b.DistanceSqrd);
            }
        }

        static CommandBuffer()
        {
            current = new CommandBuffer();
        }

        public static void Push()
        {
            stack.Push(current);

            if(freeBuffers.Count > 0)
                current = freeBuffers.Pop();
            else
                current = new CommandBuffer();
        }

        public static void Pop()
        {
            foreach (var pair in current.Passes)
                pair.Value.Clear();

            freeBuffers.Push(current);

            current = stack.Pop();
        }

        private CommandBuffer()
        {
            foreach (RenderPass pass in Enum.GetValues(typeof(RenderPass)))
                Passes.Add(pass, new Pass());
        }

        public static void Enqueue(RenderPass pass, Action callback)
        {
            current.Passes[pass].Callbacks.Push(new DrawCall()
            {
                Callback = callback
            });
        }

        public static void Enqueue(Mesh mesh, int meshPart, Material material, MaterialBlock block)
        {
            if (material == null);

            int index = Convert.ToInt32(material.UseInstancing);

            foreach (var p in material.GetPasses())
            {
                //var hash = (mesh.GetInstanceId(), material.GetInstanceId(), meshPart).GetHashCode();

                current.Passes[p.RenderPass].Quickset[index].Push(new DrawCall()
                {
                    Mesh = mesh,
                    MeshPart = meshPart,
                    Material = material,
                    Params = block,
                   // Hash = 0,
                    Key = new BatchKey { material = material.GetHashCode(), mesh = mesh.GetHashCode(), meshpart = meshPart }
                });
            }
        }

        public static void Clear()
        {
            foreach (var pair in current.Passes)
                pair.Value.Clear();
        }

        public static void ClearStatistics()
        {
            foreach (RenderPass pass in Enum.GetValues(typeof(RenderPass)))
            {
                TotalDrawNonInstanced[pass] = 0;
                TotalDrawInstanced[pass] = 0;
                TotalInstances[pass] = 0;
            }
        }

        public static int GetNonInstanced(RenderPass pass)
        {
            TotalDrawNonInstanced.TryGetValue(pass, out int result);
            return result;
        }

        public static int GetInstanced(RenderPass pass)
        {
            TotalDrawInstanced.TryGetValue(pass, out int result);
            return result;
        }

        public static int GetTotalInstances(RenderPass pass)
        {
            TotalInstances.TryGetValue(pass, out int result);
            return result;
        }

        public static void IncrementDrawNonInstanced(RenderPass pass, int amount)
        {
            TotalDrawNonInstanced[pass] += amount;
        }

        public static void IncrementDrawInstanced(RenderPass pass, int amount)
        {
            TotalDrawInstanced[pass] += amount;
        }


        public static void IncrementInstances(RenderPass pass, int amount)
        {
            TotalInstances[pass] += amount;
        }

        public static void Execute(RenderPass pass)
        {
            Profiler.Start(pass.ToString());

            var state = Engine.Device.ImmediateContext.Rasterizer.State;
            var blend = Engine.Device.ImmediateContext.OutputMerger.BlendState;
            var stencil = Engine.Device.ImmediateContext.OutputMerger.DepthStencilState;

            foreach (var pair in batches)
                pair.Value.Clear();

            int directCalls = 0;
            var p = current.Passes[pass];

            Profiler.Start("Create Batches");

            BatchKey key = new BatchKey();
            InstanceBatch batch = null;
            foreach (var drawCall in p.InstancedCalls)
            {
                if (!key.Equals(drawCall.Key))
                {
                    key = drawCall.Key;

                    if (!batches.TryGetValue(key, out batch))
                    {
                        batch = new InstanceBatch(drawCall.Mesh, drawCall.MeshPart, drawCall.Material);
                        batches.Add(key, batch);
                        batchlist.Add(batch);
                    }
                }

                batch.Add(drawCall);
            }

            Profiler.Stop();

            if (pass == RenderPass.Opaque)
            {
                Profiler.Start("Prepare Batches");
                batchlist.For(b => b.Prepare(Camera.Main.WorldPosition), true);
                Profiler.Stop();

                Profiler.Start("Sort Instanced DrawCalls");
                batchlist.For(b => b.SortDrawCalls(), true);
                Profiler.Stop();
            }

            Profiler.Start("Fill Batches");
            batchlist.For(b => b.FillArrays(), true);
            Profiler.Stop();

            Profiler.Start("Push Buffers");
            batchlist.For(b => b.WriteBuffers());
            Profiler.Stop();

            Profiler.Start("Draw Batches");
            batchlist.For(b => b.Draw(pass));
            Profiler.Stop();

            Profiler.Start("Draw Callbacks");
            foreach (var d in p.Callbacks)
                d.Callback.Invoke();
            Profiler.Stop();

            Profiler.Start("Draw Non-Instanced");
            foreach (var d in p.DrawCalls)
            {
                d.Params?.Apply(d.Material);
                d.Material.SetPass(pass);
                d.Material.Apply();
                d.Mesh.DrawImmediate(d.Mesh.MeshParts[d.MeshPart], d.Material, Engine.Device.ImmediateContext);

                directCalls++;
            }

            Graphics.SetBlendState(blend);
            Graphics.SetRasterizerState(state);
            Graphics.SetDepthStencilState(stencil);

            Profiler.Stop();

            IncrementDrawNonInstanced(pass, directCalls);

            Profiler.Stop();
        }
    }
}