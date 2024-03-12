//#define PROFILER

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;


namespace Spark
{
    public class Profiler
    {
        public static ProfilerEntry main;
        private static ProfilerEntry current = new ProfilerEntry { name = "main" };
        private static Stack<ProfilerEntry> stack = new Stack<ProfilerEntry>();

        static Profiler()
        {
            main = current;
        }

        public class ProfilerEntry
        {
            private Stopwatch clock = new Stopwatch();
            public string name;
            public float timeAccumulated;
            public float timeElapsed;
            public List<ProfilerEntry> items = new List<ProfilerEntry>();
            private Dictionary<string, ProfilerEntry> entries = new Dictionary<string, ProfilerEntry>();

            public void Start()
            {
                clock.Start();
            }

            public void Stop()
            {
                clock.Stop();
                timeAccumulated = (float)clock.Elapsed.TotalMilliseconds;
            }

            public void Update()
            {
                clock.Stop();
                clock.Reset();

                timeElapsed = timeAccumulated;
                timeAccumulated = 0;

                for (int i = 0; i < items.Count; i++)
                    items[i].Update();
            }

            public ProfilerEntry Get(string name)
            {
                if (!entries.TryGetValue(name, out ProfilerEntry result))
                {
                    result = new ProfilerEntry { name = name };
                    entries.Add(name, result);
                    items.Add(result);
                }

                return result;
            }
        }

        // [Conditional("PROFILER")]
        public static void Start(string name)
        {
            var entry = current.Get(name);

            stack.Push(current);
            current = entry;
            entry.Start();
        }

        //[Conditional("PROFILER")]
        public static void Stop()
        {
            current.Stop();
            current = stack.Pop();
        }

        //[Conditional("PROFILER")]
        public static void Update()
        {
            main.Update();
        }
    }
}  