using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark
{
    /// <summary>
    /// 
    /// </summary>
    public class AnimationCurve
    {
        public string TargetParameter;

        private FloatKeys keyframes = new FloatKeys();

        public void AddKeyFrame(float time, float value)
        {
            keyframes.Add(new FloatKey { Time = time, Value = value });
        }

        public float Evalute(float time)
        {
            return keyframes.GetValue(time);
        }
    }

    public struct FloatKey
    {
        public float Time;
        public float Value;
    }

    public class FloatKeys : List<FloatKey>
    {
        public float GetValue(float time)
        {
            if (this.Count < 1) return 0;
            if (this.Count == 1) return this[0].Value;

            int index = GetKeyframe(time);
            int next = index + 1;
            if (next > this.Count - 1)
                next = this.Count - 1;

            if (next == index) return this[index].Value;

            float delta = this[next].Time - this[index].Time;
            float factor = (time - this[index].Time) / delta;

            return MathHelper.Lerp(this[index].Value, this[next].Value, factor);

            //var keyframe0 = this[index];
            //var keyframe1 = this[next];

            //float m0 = keyframe0.outTangent * delta;
            //float m1 = keyframe1.inTangent * delta;

            //float t2 = time * time;
            //float t3 = t2 * time;

            //float a = 2 * t3 - 3 * t2 + 1;
            //float b = t3 - 2 * t2 + time;
            //float c = t3 - t2;
            //float d = -2 * t3 + 3 * t2;

            //return a * keyframe0.Value + b * m0 + c * m1 + d * keyframe1.Value;
        }

        private int GetKeyframe(float time)
        {
            for (int i = 0; i < this.Count; i++)
            {
                if (this[i].Time == time)
                    return i;
                else if (this[i].Time > time)
                    return Math.Max(0, i - 1);
            }

            return 0;
        }
    }
}
