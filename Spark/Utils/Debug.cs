using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;

namespace Spark
{
    public static class Debug
    {
        public struct LogEntry
        {
            public string Message;
            public StackTrace StackTrace;
        }

        private static readonly List<LogEntry> lines = new List<LogEntry>();

        public static readonly ReadOnlyCollection<LogEntry> Lines = new ReadOnlyCollection<LogEntry>(lines);

        public static event Action<string, StackTrace> OnLog;

        static Debug()
        {
        }

        public static void ClearLog() => lines.Clear();
       
        static void Append(string message, StackTrace stackTrace) => lines.Add(new LogEntry
        {
            StackTrace = stackTrace,
            Message = message
        });

        public static void Assert(bool condition)
        {
            if (condition) return;
            var st = new StackTrace(1, true);
            Append("Assert", st);
        }

        public static void Assert(bool condition, string message)
        {
            if (condition) return;

            var st = new StackTrace(1, true);
            Append(message, st);
            OnLog?.Invoke(message, st);
        }

        public static void Log(string text)
        {
            var st = new StackTrace(1, true);
            Append(text, st);
            OnLog?.Invoke(text, st);
        }

        public static string StackFramesToString(this StackFrame[] frames, int numMethodsToSkip = 0)
        {
            bool displayFilenames = true;   // we'll try, but demand may fail
            String word_At = "at";
            String inFileLineNum = "in {0}:line {1}";

            bool fFirstFrame = true;
            StringBuilder sb = new StringBuilder(255);
            for (int iFrameIndex = 0; iFrameIndex < frames.Count(); iFrameIndex++)
            {
                StackFrame sf = GetFrame(iFrameIndex, frames, numMethodsToSkip);
                MethodBase mb = sf.GetMethod();
                if (mb != null)
                {
                    // We want a newline at the end of every line except for the last
                    if (fFirstFrame)
                        fFirstFrame = false;
                    else
                        sb.Append(Environment.NewLine);

                    sb.AppendFormat(CultureInfo.InvariantCulture, "   {0} ", word_At);

                    Type t = mb.DeclaringType;
                    // if there is a type (non global method) print it
                    if (t != null)
                    {
                        sb.Append(t.FullName.Replace('+', '.'));
                        sb.Append(".");
                    }
                    sb.Append(mb.Name);

                    // deal with the generic portion of the method
                    if (mb is MethodInfo && ((MethodInfo)mb).IsGenericMethod)
                    {
                        Type[] typars = ((MethodInfo)mb).GetGenericArguments();
                        sb.Append("[");
                        int k = 0;
                        bool fFirstTyParam = true;
                        while (k < typars.Length)
                        {
                            if (fFirstTyParam == false)
                                sb.Append(",");
                            else
                                fFirstTyParam = false;

                            sb.Append(typars[k].Name);
                            k++;
                        }
                        sb.Append("]");
                    }

                    // arguments printing
                    sb.Append("(");
                    ParameterInfo[] pi = mb.GetParameters();
                    bool fFirstParam = true;
                    for (int j = 0; j < pi.Length; j++)
                    {
                        if (fFirstParam == false)
                            sb.Append(", ");
                        else
                            fFirstParam = false;

                        String typeName = "<UnknownType>";
                        if (pi[j].ParameterType != null)
                            typeName = pi[j].ParameterType.Name;
                        sb.Append(typeName + " " + pi[j].Name);
                    }
                    sb.Append(")");

                    // source location printing
                    if (displayFilenames && (sf.GetILOffset() != -1))
                    {
                        // If we don't have a PDB or PDB-reading is disabled for the module,
                        // then the file name will be null.
                        String fileName = null;

                        // Getting the filename from a StackFrame is a privileged operation - we won't want
                        // to disclose full path names to arbitrarily untrusted code.  Rather than just omit
                        // this we could probably trim to just the filename so it's still mostly usefull.
                        try
                        {
                            fileName = sf.GetFileName();
                        }
                        catch
                        {
                            // If the demand for displaying filenames fails, then it won't
                            // succeed later in the loop.  Avoid repeated exceptions by not trying again.
                            displayFilenames = false;
                        }

                        if (fileName != null)
                        {
                            // tack on " in c:\tmp\MyFile.cs:line 5"
                            sb.Append(' ');
                            sb.AppendFormat(CultureInfo.InvariantCulture, inFileLineNum, fileName, sf.GetFileLineNumber());
                        }
                    }
                }
            }

            return sb.ToString();
        }

        public static StackFrame GetFrame(int index, StackFrame[] frames, int numMethodsToSkip)
        {
            if ((frames != null) && (index < frames.Count()) && (index >= 0))
                return frames[index + numMethodsToSkip];

            return null;
        }
    }
}
