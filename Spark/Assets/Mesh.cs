using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX.DXGI;
using System.ComponentModel;
using System.IO;
using System.Threading;

namespace Spark
{
    [Serializable]
    public class MeshPart
    {
        public string Name = string.Empty;
        public bool Enabled = true;

        [ReadOnly(true)]
        public int BaseVertex;
        [ReadOnly(true)]
        public int BaseIndex;
        [ReadOnly(true)]
        public int NumIndices;

        public void Write(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(Enabled);
            writer.Write(BaseVertex);
            writer.Write(BaseIndex);
            writer.Write(NumIndices);
        }

        public void Read(BinaryReader reader)
        {
            Name = reader.ReadString();
            Enabled = reader.ReadBoolean();
            BaseVertex = reader.ReadInt32();
            BaseIndex = reader.ReadInt32();
            NumIndices = reader.ReadInt32();
        }
    }

    public partial class Mesh : Asset, IDisposable
    {
        private static volatile int _instanceCount;

        private readonly int _instanceId = Interlocked.Increment(ref _instanceCount);
        private readonly VertexBufferBinding[] bindings = new VertexBufferBinding[15];
        private BitSet dirtyFlags = new BitSet(1);
        private bool dirtyIndices;

        private int[] indices;
        private Vector3[] vertices;
        private Vector3[] normals;
        private Vector3[] tangents;
        private Vector3[] biNormals;
        private Vector4[] colors;
        private Vector2[] uV;
        private Vector2[] uV1;
        private Vector2[] uV2;
        private BoneWeight[] boneweights;
        private Buffer indexBuffer;

        public Bone[] Bones;

        public Buffer IndexBuffer => indexBuffer;

        public int GetInstanceId() => _instanceId;

        public BoundingBox BoundingBox;

        public Matrix RootRotation = Matrix.Identity;

        public List<MeshPart> MeshParts = new List<MeshPart>();

        public PrimitiveTopology Topology = PrimitiveTopology.TriangleList;

        public InputElement[] InputElements = UniversalVertex.InputElements;

        public int[] Indices
        {
            get => indices;
            set { indices = value; dirtyIndices = true; }
        }

        public Vector3[] Vertices
        {
            get => vertices;
            set { vertices = value; dirtyFlags.SetBit(0); }
        }

        public Vector3[] Normals
        {
            get => normals;
            set { normals = value; dirtyFlags.SetBit(1); }
        }

        public Vector3[] Tangents
        {
            get => tangents;
            set { tangents = value; dirtyFlags.SetBit(2); }
        }

        public Vector3[] BiNormals
        {
            get => biNormals;
            set { biNormals = value; dirtyFlags.SetBit(3); }
        }

        public Vector4[] Colors
        {
            get => colors;
            set { colors = value; dirtyFlags.SetBit(4); }
        }

        public Vector2[] UV
        {
            get => uV;
            set { uV = value; dirtyFlags.SetBit(5); }
        }

        public Vector2[] UV1
        {
            get => uV1;
            set { uV1 = value; dirtyFlags.SetBit(6); }
        }

        public Vector2[] UV2
        {
            get => uV2;
            set { uV2 = value; dirtyFlags.SetBit(7); }
        }

        public BoneWeight[] Boneweights
        {
            get => boneweights;
            set { boneweights = value; dirtyFlags.SetBit(8); }
        }

        public VertexBufferBinding[] Bindings
        {
            get
            {
                if (dirtyIndices)
                    UpdateIndexBuffer();
                if (!dirtyFlags.IsClear)
                    UpdateVertexBuffers();
                return bindings;
            }
        }

        public Mesh() { }

        private void UpdateIndexBuffer()
        {
            Disposer.SafeDispose(ref indexBuffer);
            indexBuffer = Geometry.CreateIndexBuffer(indices);
            dirtyIndices = false;
        }

        private void UpdateVertexBuffers()
        {
            RecreateBuffer(0, vertices);
            RecreateBuffer(1, normals);
            RecreateBuffer(2, tangents);
            RecreateBuffer(3, biNormals);
            RecreateBuffer(4, colors);
            RecreateBuffer(5, uV);
            RecreateBuffer(6, uV1);
            RecreateBuffer(7, uV2);
            RecreateBuffer(8, boneweights);

            InputElements = UniversalVertex.InputElements;

            dirtyFlags.Clear();
        }

        private void RecreateBuffer<T>(int slot, T[] array) where T : struct
        {
            if (!dirtyFlags.IsBitSet(slot)) return;
            ref var binding = ref bindings[slot];
            var buffer = binding.Buffer;
            Disposer.SafeDispose(ref buffer);
            binding.Buffer = array != null ? Geometry.CreateVertexBuffer(array) : null;
            binding.Stride = array != null ? Utilities.SizeOf<T>() : 0;
        }

        public void Dispose()
        {
            MeshParts.Clear();

            Bones = null;
            Disposer.SafeDispose(ref indexBuffer);
        
            foreach(var binding in bindings)
                binding.Buffer?.Dispose();
        }
    }
}