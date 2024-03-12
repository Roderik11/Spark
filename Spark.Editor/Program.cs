using System;
using System.Threading;
using System.Globalization;

namespace Spark.Editor
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var app = new EditorApp();
            app.Run();
        }
    }
}