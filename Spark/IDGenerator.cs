using System;
using System.Security.Cryptography;

namespace Spark
{
    public static class IDGenerator
    {
        private static int _idCounter;
        private readonly static RandomNumberGenerator rng;

        static IDGenerator()
        {
            rng = RandomNumberGenerator.Create();
        }

        public static int GetId()
        {
            return System.Threading.Interlocked.Increment(ref _idCounter);
        }

        public static ulong GetUID()
        {
            var bytes = new byte[8];
            rng.GetBytes(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }
    }
}
