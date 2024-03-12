using System;
using System.Collections.Generic;
using SharpDX;

namespace Spark
{
    public struct VectorKey
    {
        public float Time;
        public Vector3 Value;
    }

    public class VectorKeys //: List<VectorKey>
    {
        private VectorKey[] array;

        public VectorKeys(List<VectorKey> list)
        {
            array = list.ToArray();
            Count = array.Length;
        }

        public int Count { get; private set; }

        public Vector3 GetValue(float time)
        {
            if (Count < 1) return Vector3.One;
            if (Count == 1) return this[0].Value;

            int index = GetKeyframe(time);
            int next = index + 1;
            if (next > Count - 1)
                next = 0;

            if (next == index) return this[index].Value;

            float delta = this[next].Time - this[index].Time;
            float factor = (time - this[index].Time) / delta;

            return Vector3.Lerp(this[index].Value, this[next].Value, factor);
        }

        public void GetValue(float time, ref Vector3 result)
        {
            if (Count < 1) return;
            if (Count == 1)
            {
                result = ref this[0].Value;
                return;
            }

            int index = GetKeyframe(time);
            int next = index + 1;
            if (next > Count - 1)
                next = 0;

            if (next == index)
            {
                result = ref this[index].Value;
                return;
            }

            ref VectorKey thisFrame = ref this[index];
            ref VectorKey nextFrame = ref this[next];

            float delta = nextFrame.Time - thisFrame.Time;
            float factor = (time - thisFrame.Time) / delta;

            Vector3.Lerp(ref thisFrame.Value, ref nextFrame.Value, factor, out result);

            return;

            int prev = index - 1;
            if (prev < 0) prev = 0;
            ref VectorKey prevFrame = ref this[prev];
            var t0 = 0.5f * (thisFrame.Value - prevFrame.Value);
            var t1 = 0.5f * (nextFrame.Value - thisFrame.Value);
            result = Vector3.Hermite(thisFrame.Value, t0, nextFrame.Value, t1, factor);
        }

        public VectorKey Last()
        {
            return this[Count - 1];
        }

        public ref VectorKey this[int i] => ref array[i];

        private int GetKeyframe(float time)
        {
            for (int i = 0; i < this.Count; i++)
            {
                ref var frameTime = ref this[i].Time;

                if (frameTime < time)
                    continue;

                if (frameTime == time)
                    return i;

                if (frameTime > time)
                    return i < 2 ? 0 : i - 1;
            }

            return 0;
        }
    }
}
