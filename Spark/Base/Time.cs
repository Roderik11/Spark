using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spark
{
    public static class Time
    {
        /// <summary>
        /// Time delta in milliseconds
        /// </summary>
        public static float DeltaMilliseconds { get; private set; }
        
        /// <summary>
        /// Time delta in seconds
        /// </summary>
        public static float Delta { get; private set; }
        
        /// <summary>
        /// Smoothly interpolated Delta in seconds
        /// </summary>
        public static float SmoothDelta { get; private set; }
       
        /// <summary>
        /// Total time passed since start in second
        /// </summary>
        public static float TotalTime { get; private set; }

        /// <summary>
        /// Frames per second
        /// </summary>
        public static float FPS { get; private set; }

        private static float previousDelta = .16f;

        private static float fpsCounter;
        private static float fpsInterval;
        private static readonly Stopwatch clock = new Stopwatch();
        private const float MAXDELTA = 1000f / 30f;

        static Time()
        {
            Delta = float.Epsilon;
        }

        internal static void StartTime()
        {
            clock.Reset();
            clock.Start();
        }

        internal static void EndTime()
        {
            clock.Stop();

            var elapsed = (float)clock.Elapsed.TotalMilliseconds;
            
            DeltaMilliseconds = Math.Min(elapsed, MAXDELTA);
            Delta = DeltaMilliseconds * 0.001f;
            TotalTime += Delta;

            float f = 0.2f;
            SmoothDelta = f * Delta + (1 - f) * previousDelta;
            previousDelta = Delta;

            UpdateFPS();
        }

        private static void UpdateFPS()
        {
            fpsCounter++;
            fpsInterval += DeltaMilliseconds;

            if (fpsInterval >= 1000)
            {
                FPS = fpsCounter;
                fpsInterval = fpsCounter = 0;
            }
        }
    }

}
