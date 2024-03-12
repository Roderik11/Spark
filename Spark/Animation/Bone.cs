using System;
using System.Collections.Generic;
using System.IO;
using SharpDX;

namespace Spark
{
    public class Bone
    {
        private string _name;
        public int Hash { get; private set; }
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                Hash = string.IsNullOrEmpty(value) ? 0 : _name.GetHashCode();
            }
        }

        public BonePose BindPose;
        public Matrix BindPoseMatrix;
        public Matrix OffsetMatrix;
        public int Parent;

        public override string ToString()
        {
            return Name;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(_name);
            writer.Write(BindPose);
            writer.Write(BindPoseMatrix);
            writer.Write(OffsetMatrix);
            writer.Write(Parent);
        }

        public void Read(BinaryReader reader)
        {
            Name = reader.ReadString();
            BindPose = reader.Read<BonePose>();
            BindPoseMatrix = reader.Read<Matrix>();
            OffsetMatrix = reader.Read<Matrix>();
            Parent = reader.ReadInt32();
        }

    }

    public struct BonePose
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    public struct BoneWeight
    {
        public Int4 BoneIDs;
        public Vector4 Weights;
    }


    //public struct BoneTransform
    //{
    //    public int Parent;
    //    public string Name;
    //    public Matrix Transform;
    //    public Matrix World;
    //}
}
