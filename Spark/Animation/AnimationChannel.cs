using System;
using System.Collections.Generic;
using SharpDX;

namespace Spark
{
    public class AnimationChannel
    {
        private string _target;
        public int Hash { get; private set; }

        public event Action OnHashChanged;

        public string Target
        {
            get { return _target; }
            set
            {
                _target = value;
                Hash = string.IsNullOrEmpty(value) ? 0 : _target.GetHashCode();
                OnHashChanged?.Invoke();
            }
        }

        public VectorKeys Position;
        public QuaternionKeys Rotation;
        public VectorKeys Scale;

        //public Matrix GetTransform(Bone bone, float time)
        //{
        //    Vector3 scale = Scale.Count > 0 ? Scale.GetValue(time) : Vector3.One;
        //    Vector3 position = Position.Count > 0 ? Position.GetValue(time) : bone.BindPose.Position;
        //    Quaternion rotation = Rotation.Count > 0 ? Rotation.GetValue(time) : Quaternion.Identity;
        //    return Matrix.Scaling(scale) * Matrix.RotationQuaternion(rotation) * Matrix.Translation(position);
        //}

        public void GetBoneTransform(Bone bone, float time, ref BonePose result)
        {
            Scale.GetValue(time, ref result.Scale);
            Position.GetValue(time, ref result.Position);
            Rotation.GetValue(time, ref result.Rotation);
        }

        public BonePose GetBoneTransform(Bone bone, float time)
        {
            return new BonePose
            {
                Scale = Scale.GetValue(time),
                Position = Position.GetValue(time),
                Rotation = Rotation.GetValue(time)
            };
        }

        public AnimationChannel CopyRange(float start, float end)
        {
            var position = new List<VectorKey>();
            var scale = new List<VectorKey>();
            var rotation = new List<QuaternionKey>();


            for (int i = 0; i < Position.Count; i++)
            {
                if (Position[i].Time >= start && Position[i].Time <= end)
                {
                    VectorKey key = Position[i];
                    key.Time = key.Time - start;
                    position.Add(key);
                }
            }

            for (int i = 0; i < Rotation.Count; i++)
            {
                if (Rotation[i].Time >= start && Rotation[i].Time <= end)
                {
                    QuaternionKey key = Rotation[i];
                    key.Time = key.Time - start;
                    rotation.Add(key);
                }
            }

            for (int i = 0; i < Scale.Count; i++)
            {
                if (Scale[i].Time >= start && Scale[i].Time <= end)
                {
                    VectorKey key = Scale[i];
                    key.Time = key.Time - start;
                    scale.Add(key);
                }
            }

            AnimationChannel copy = new AnimationChannel
            {
                Target = Target,
                Position = new VectorKeys(position),
                Scale = new VectorKeys(scale),
                Rotation = new QuaternionKeys(rotation),
            };
            return copy;
        }
    }
}
