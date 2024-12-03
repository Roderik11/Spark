using System;
using System.Collections.Generic;
using SharpDX;

namespace Spark
{
    public delegate void BoneTransformHandler(Bone bone, ref Matrix matrix);

    public class Animator : Component, IUpdate
    {
        public Animation Animation;
        public Dictionary<string, float> Parameters = new Dictionary<string, float>();
        public event BoneTransformHandler OnBoneTransform;

        public void Update()
        {
            foreach (AnimationLayer layer in Animation.Layers)
                layer.Update(this);
        }

        public float GetParam(string name)
        {
            Parameters.TryGetValue(name, out var result);
            return result;
        }

        public void SetParam(string name, float value)
        {
            if (!Parameters.ContainsKey(name)) return;
            Parameters[name] = value;
        }

        void GPUPosingIdea()
        {
            // give to compute shader:
            // skeleton (bind pose)
            // offset matrices 
            // all active animations

            // pass 1: pose
            // foreach bone
            // foreach clip affecting it
            // lerp to clip pose by weight

            // pass2: hierarchy
            // foreach bone
            // mul with parent
        }

        public Matrix[] GetPose(ref Bone[] skeleton)
        {
            Matrix[] bones = new Matrix[skeleton.Length];
            Matrix[] world = new Matrix[skeleton.Length];
            Bone bone;
            BonePose tempPose = new BonePose { Scale = Vector3.One };

            int count = skeleton.Length;

            // blend poses
            for (int i = 0; i < count; i++)
                BlendBone(skeleton[i], ref tempPose, out bones[i]);

            // multiply hierarchy
            for (int i = 0; i < count; i++)
            {
                bone = skeleton[i];

                if (bone.Parent > -1)
                    Matrix.Multiply(ref bones[i], ref bones[bone.Parent], out bones[i]);

                Matrix.Multiply(ref skeleton[i].OffsetMatrix, ref bones[i], out world[i]);
            }

            return world;
        }

        public void GetPose(Bone[] skeleton, Matrix[] bones)
        {
            int count = skeleton.Length;

            // blend poses
            BonePose tempPose = new BonePose { Scale = Vector3.One };
            for (int i = 0; i < count; i++)
                BlendBone(skeleton[i], ref tempPose, out bones[i]);

            // retargeting
            //for (int i = 0; i < count; i++)
            //{
            //    var bone = skeleton[i];

            //    //var sourceBoneLength = bones[i].TranslationVector.Length();
            //    //var myBoneLength = bone.BindPose.Position.Length();

            //    if (skeleton[i].Parent > -1)
            //    {
            //        var sourceBoneLength = Vector3.Distance(bones[i].TranslationVector, bones[bone.Parent].TranslationVector);
            //        var myBoneLength = Vector3.Distance(bone.BindPose.Position, skeleton[bone.Parent].BindPose.Position);

            //        var ratio = myBoneLength / sourceBoneLength;
            //        var b = bones[i];
            //        b.TranslationVector = Vector3.Multiply(b.TranslationVector, ratio);
            //        bones[i] = b;
            //    }
            //}

            // apply custom bone transforms
            if (OnBoneTransform != null)
            {
                for (int i = 0; i < count; i++)
                    OnBoneTransform(skeleton[i], ref bones[i]);
            }

            // multiply hierarchy
            for (int i = 0; i < count; i++)
            {
                if (skeleton[i].Parent > -1)
                    Matrix.Multiply(ref bones[i], ref bones[skeleton[i].Parent], out bones[i]);
            }

            // apply bone offset matrix
            for (int i = 0; i < count; i++)
                Matrix.Multiply(ref skeleton[i].OffsetMatrix, ref bones[i], out bones[i]);

        }

        /// <summary>
        /// Multithreaded using homemade Parallel
        /// </summary>
        public void GetPoseParallel(Bone[] skeleton, Matrix[] bones)
        {
            int count = skeleton.Length;

            // blend poses
            Profiler.Start("BlendBone");
            Parallel.For(0, count, (i) =>
            {
                BonePose tempPose = new BonePose { Scale = Vector3.One };
                BlendBone(skeleton[i], ref tempPose, out bones[i]);
            });
            Profiler.Stop();

            // apply custom bone transforms
            if (OnBoneTransform != null)
            {
                for (int i = 0; i < count; i++)
                    OnBoneTransform(skeleton[i], ref bones[i]);
            }

            // multiply hierarchy
            Profiler.Start("MUL Hierarchy");
            for (int i = 0; i < count; i++)
            {
                if (skeleton[i].Parent > -1)
                    Matrix.Multiply(ref bones[i], ref bones[skeleton[i].Parent], out bones[i]);
            }
            Profiler.Stop();

            // apply bone offset matrix
            Profiler.Start("MUL Offset");
            for (int i = 0; i < count; i++)
                Matrix.Multiply(ref skeleton[i].OffsetMatrix, ref bones[i], out bones[i]);
            Profiler.Stop();
        }

        /// <summary>
        /// Multithreaded using System.Threading.Task
        /// </summary>
        public void GetPose3(Bone[] skeleton, Matrix[] bones)
        {
            Bone bone;
            int count = skeleton.Length;

            // blend poses
            System.Threading.Tasks.Parallel.For(0, count, (i) =>
            {
                BonePose tempPose = new BonePose { Scale = Vector3.One };
                BlendBone(skeleton[i], ref tempPose, out bones[i]);
            });

            // apply custom bone transforms
            if (OnBoneTransform != null)
            {
                for (int i = 0; i < count; i++)
                {
                    bone = skeleton[i];
                    OnBoneTransform(bone, ref bones[i]);
                }
            }

            // multiply hierarchy
            for (int i = 0; i < count; i++)
            {
                if (skeleton[i].Parent > -1)
                    Matrix.Multiply(ref bones[i], ref bones[skeleton[i].Parent], out bones[i]);
            }

            // apply bone offset matrix
            System.Threading.Tasks.Parallel.For(0, count, (i) =>
            {
                Matrix.Multiply(ref skeleton[i].OffsetMatrix, ref bones[i], out bones[i]);
            });
        }

        private void BlendBone(Bone bone, ref BonePose temp, out Matrix matrix)
        {
            AnimationLayer layer;
            int layerCount = Animation.Layers.Count;

            BonePose blendPose = bone.BindPose;

            for (int i = 0; i < layerCount; i++)
            {
                layer = Animation.Layers[i];

                // masking
                if (layer.Mask != null && !layer.Mask.Contains(bone.Name))
                    continue;

                int stateCount = layer.GetStateCount();

                for (int st = 0; st < stateCount; st++)
                {
                    var state = layer.GetState(st);
                    state?.BlendBone(bone, ref blendPose, ref temp);
                }
            }

            Matrix.Scaling(ref blendPose.Scale, out var mat1);
            Matrix.RotationQuaternion(ref blendPose.Rotation, out var mat2);
            Matrix.Multiply(ref mat1, ref mat2, out mat1);
            Matrix.Translation(ref blendPose.Position, out mat2);
            Matrix.Multiply(ref mat1, ref mat2, out matrix);
        }
    
    }
}