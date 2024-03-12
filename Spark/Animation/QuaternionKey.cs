using System;
using System.Collections.Generic;
using SharpDX;

namespace Spark
{
    public struct QuaternionKey
    {
        public float Time;
        public Quaternion Value;
    }

    public class QuaternionKeys
    {
        private QuaternionKey[] array;
        public int Count { get; private set; }

        public QuaternionKeys(List<QuaternionKey> list)
        {
            array = list.ToArray();
            Count = array.Length;
        }

        public Quaternion GetValue(float time)
        {
            if (Count < 1) return Quaternion.Identity;
            if (Count == 1) return this[0].Value;

            int index = GetKeyframe(time);
            int next = index + 1;
            if (next > Count - 1)
                next = 0;

            if (next == index) return this[index].Value;

            float delta = this[next].Time - this[index].Time;
            float factor = (time - this[index].Time) / delta;

            return Quaternion.Lerp(this[index].Value, this[next].Value, factor);
        }

        public void GetValue(float time, ref Quaternion result)
        {
            if (Count < 1)
                return;

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

            ref QuaternionKey thisFrame = ref this[index];
            ref QuaternionKey nextFrame = ref this[next];

            float delta = nextFrame.Time - thisFrame.Time;
            float factor = (time - thisFrame.Time) / delta;

            Quaternion.Slerp(ref thisFrame.Value, ref nextFrame.Value, factor, out result);
        }

        public QuaternionKey Last()
        {
            return this[Count - 1];
        }

        public ref QuaternionKey this[int i] =>  ref array[i];

        private int GetKeyframe(float time)
        {
            for (int i = 0; i < Count; i++)
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



    public class QuaternionKeys2 : List<QuaternionKey>
    {
        public Quaternion GetValue(float time)
        {
            if (Count < 1) return Quaternion.Identity;
            if (Count == 1) return this[0].Value;

            int index = GetKeyframe(time);
            int next = index + 1;
            if (next > Count - 1)
                next = 0;

            float delta = this[next].Time - this[index].Time;
            float factor = (time - this[index].Time) / delta;

            return Quaternion.Lerp(this[index].Value, this[next].Value, factor);
        }

        private int GetKeyframe(float time)
        {
            for (int i = 0; i < Count; i++)
            {
                if (this[i].Time == time)
                    return i;

                if (this[i].Time > time)
                    return Math.Max(0, i - 1);
            }

            return 0;
        }
    }
}
