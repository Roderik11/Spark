using System;
using System.Collections.Generic;
using SharpDX;

namespace Spark
{
    [Serializable]
    public class AnimationState
    {
        public string Name;
        public AnimationClip Clip;
        public float Speed = 1f;
        public bool Loop;

        public List<AnimationCurve> Curves = new List<AnimationCurve>();

        internal float Weight = 1;

        protected float timeElapsed;

        public float TimeElapsed => timeElapsed;

        public float TimeNormalized
        {
            get
            {
                if (Clip == null) return 0;
                return timeElapsed * Clip.DurationSecondsInverse;
            }
        }

        public void SetTimeElapsed(float value)
        {
            timeElapsed = value;
        }

        public void SetTimeNormalized(float value)
        {
            if (Clip == null) return;
            timeElapsed = Clip.DurationSeconds * MathUtil.Clamp(value, 0, 1);
        }

        public virtual int GetStateCount() => 0;
        public virtual AnimationState GetState(int index) => this;

        public virtual void Update(Animator animator)
        {
            if (Loop)
            {
                timeElapsed += Time.SmoothDelta * Speed;

                if (timeElapsed > Clip.DurationSeconds)
                    timeElapsed = 0;
            }
            else
            {
                if(timeElapsed < Clip.DurationSeconds)
                    timeElapsed += Time.SmoothDelta * Speed;

                if (timeElapsed > Clip.DurationSeconds)
                    timeElapsed = Clip.DurationSeconds;
            }

            foreach (var curve in Curves)
            {
                float value = curve.Evalute(TimeNormalized);
                animator.SetParam(curve.TargetParameter, value);
            }
        }

        internal void BlendBone(Bone bone, ref BonePose blendPose, ref BonePose temp)
        {
            if (Weight <= 0) return;

            if (Clip != null)
            {
                Clip.GetBonePose(bone, TimeElapsed, ref temp);
                Vector3.Lerp(ref blendPose.Position, ref temp.Position, Weight, out blendPose.Position);
                Vector3.Lerp(ref blendPose.Scale, ref temp.Scale, Weight, out blendPose.Scale);
                Quaternion.Lerp(ref blendPose.Rotation, ref temp.Rotation, Weight, out blendPose.Rotation);
                return;
            }

            int stateCount = GetStateCount();
            if (stateCount > 0)
            {
                for (int i = 0; i < stateCount; i++)
                    GetState(i).BlendBone(bone, ref blendPose, ref temp);
            }
        }
    }
}
