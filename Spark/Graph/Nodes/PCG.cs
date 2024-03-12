using System;
using System.Collections.Generic;
using System.ComponentModel;
using SharpDX;
using Spark.Graph;

namespace Spark.PCG
{
    public class SamplePointSet
    {
        public Vector3[] position;
        public Vector3[] extents;
        public Matrix[] transform;
        public float[] density;
    }

    [Category("Sampler")]
    public class Sampler : Node
    {
        [Output]
        public SamplePointSet Output;
    }

    [Category("Math")]
    public class MakeVector : Node
    {
        [Input]
        public float X;

        [Input]
        public float Y;

        [Input]
        public float Z;

        [Output]
        public Vector3 Vector;
    }

    [Category("Tranform")]
    public class BoundsModifier : Node
    {
        [Input]
        public SamplePointSet Input;

        public Vector3 BoundsMin;
        public Vector3 BoundsMax;

        [Output]
        public SamplePointSet Output;
    }

    [Category("Tranform")]
    public class TranformPoints: Node
    {
        [Input]
        public SamplePointSet Input;

        public Vector3 OffsetMin;
        public Vector3 OffsetMax;

        public bool AbsoluteOffset;

        public Quaternion RotationMin = Quaternion.Identity;
        public Quaternion RotationMax = Quaternion.Identity;

        public bool AbsoluteRotation;

        public Vector3 ScaleMin = Vector3.One;
        public Vector3 ScaleMax = Vector3.One;

        public bool AbsoluteScale;

        [Output]
        public SamplePointSet Output;
    }

    [Category("Noise")]
    public class AttributeNoise : Node
    {
        [Input]
        public SamplePointSet Input;

        public float NoiseMin;
        public float MoiseMax;

        [Output]
        public SamplePointSet Output;
    }

    [Category("Filter")]
    public class DensityFilter : Node
    {
        [Input]
        public SamplePointSet Input;

        public float LowerBound;
        public float UpperBound;

        [Output]
        public SamplePointSet Output;
    }

    [Category("Filter")]
    public class PointFilter : Node
    {
        [Input]
        public SamplePointSet Input;

        public float LowerBound;
        public float UpperBound;

        [Output]
        public SamplePointSet Output;
    }


    [Category("Noise")]
    public class DensityNoise : Node
    {
        [Input]
        public SamplePointSet Input;

        public float LowerBound;
        public float UpperBound;

        [Output]
        public SamplePointSet Output;
    }

    [Category("Filter")]
    public class Difference: Node
    {
        [Input]
        public SamplePointSet InputA;

        [Input]
        public SamplePointSet InputB;

        public DensityFunction DensityFunction = DensityFunction.Minimum;

        [Output]
        public SamplePointSet Output;
    }

    [Category("Density")]
    public class RemapDensity : Node
    {
        [Input]
        public SamplePointSet Input;

        public Vector2 OldRange;
        public Vector2 NewRange;

        [Output]
        public SamplePointSet Output;
    }

    [Category("Filter")]
    public class Union : Node
    {
        [Input]
        public SamplePointSet InputA;

        [Input]
        public SamplePointSet InputB;

        public DensityFunction DensityFunction = DensityFunction.Maximum;

        [Output]
        public SamplePointSet Output;
    }

    [Category("Spawn")]
    public class SpawnMesh : Node
    {
        [Serializable]
        public class MeshElement
        {
            public Mesh Mesh;
            public float Weight = 1;
        }

        [Input]
        public SamplePointSet Input;

        public List<MeshElement> Meshes = new List<MeshElement>();

        [Output]
        public SamplePointSet Output;
    }

    [Category("Spawn")]
    public class SpawnEntity : Node
    {
        [Serializable]
        public class EntityElement
        {
            public Entity Mesh;
            public float Weight = 1;
        }

        [Input]
        public SamplePointSet Input;

        public List<EntityElement> Entities = new List<EntityElement>();

        [Output]
        public SamplePointSet Output;
    }

    [Category("Filter")]
    public class SelfPruning : Node
    {
        [Input]
        public SamplePointSet Input;

        [Output]
        public SamplePointSet Output;
    }

    public enum DensityFunction
    {
        Maximum,
        Minimum
    }
}
