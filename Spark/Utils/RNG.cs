using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark
{
    public static class RNG
    {
        static Random random = new Random();
        static readonly Stack<Random> stack = new Stack<Random>();

        public static void Push(int seed)
        {
            stack.Push(random);
            random =  new Random(seed);
        }

        public static void Pop()
        {
            if (stack.Count == 0) return;
            random = stack.Pop();
        }
        public static int RangeInt(int min, int max) => min + random.Next(max - min);
        public static float RangeFloat(float min, float max) => min + NextFloat() * (max - min);
        public static int NextInt() => random.Next();
        public static int NextInt(int max) => random.Next(max);      
        public static float NextFloat() => (float)random.NextDouble();
        public static double NextDouble() => random.NextDouble();
    }
}
