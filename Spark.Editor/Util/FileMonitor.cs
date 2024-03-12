using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace Spark.Editor
{
    public delegate void FileSystemEvent(String path);

    public interface IDirectoryMonitor
    {
        event FileSystemEvent Change;
        void Start();
    }

    public class DirectoryMonitor : IDirectoryMonitor
    {
        private readonly FileSystemWatcher Watcher = new FileSystemWatcher();
        private readonly Dictionary<string, DateTime> PendingEvents = new Dictionary<string, DateTime>();
        private readonly Timer Timer;
        private bool TimerStarted = false;

        public bool Enabled
        {
            get { return Watcher.EnableRaisingEvents; }
            set { Watcher.EnableRaisingEvents = value; }
        }

        public DirectoryMonitor(string dirPath)
        {
            Watcher.Path = dirPath;
            Watcher.IncludeSubdirectories = true;
            Watcher.Created += new FileSystemEventHandler(OnChange);
            Watcher.Changed += new FileSystemEventHandler(OnChange);
            Watcher.Deleted += new FileSystemEventHandler(OnChange);
          
            Timer = new Timer(OnTimeout, null, Timeout.Infinite, Timeout.Infinite);
        }

        public event FileSystemEvent Change;

        public void Start()
        {
            Watcher.EnableRaisingEvents = true;
        }

        private void OnChange(object sender, FileSystemEventArgs e)
        {
            // Don't want other threads messing with the pending events right now
            lock (PendingEvents)
            {
                // Save a timestamp for the most recent event for this path
                PendingEvents[e.FullPath] = DateTime.Now;

                // Start a timer if not already started
                if (!TimerStarted)
                {
                    Timer.Change(100, 100);
                    TimerStarted = true;
                }
            }
        }

        private void OnTimeout(object state)
        {
            List<string> paths;

            // Don't want other threads messing with the pending events right now
            lock (PendingEvents)
            {
                // Get a list of all paths that should have events thrown
                paths = FindReadyPaths(PendingEvents);

                // Remove paths that are going to be used now
                paths.ForEach(delegate(string path)
                {
                    PendingEvents.Remove(path);
                });

                // Stop the timer if there are no more events pending
                if (PendingEvents.Count == 0)
                {
                    Timer.Change(Timeout.Infinite, Timeout.Infinite);
                    TimerStarted = false;
                }
            }

            // Fire an event for each path that has changed
            paths.ForEach(delegate(string path)
            {
                FireEvent(path);
            });
        }

        private List<string> FindReadyPaths(Dictionary<string, DateTime> events)
        {
            List<string> results = new List<string>();
            DateTime now = DateTime.Now;

            foreach (KeyValuePair<string, DateTime> entry in events)
            {
                // If the path has not received a new event in the last 75ms
                // an event for the path should be fired
                double diff = now.Subtract(entry.Value).TotalMilliseconds;
                if (diff >= 75)
                {
                    results.Add(entry.Key);
                }
            }

            return results;
        }

        private void FireEvent(string path)
        {
            FileSystemEvent evt = Change;
            if (evt != null)
            {
                evt(path);
            }
        }
    }
}
