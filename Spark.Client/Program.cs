using System;
using System.Threading;
using System.Globalization;
using System.Windows.Forms;
using Spark;
using Squid;
using System.Collections.Generic;
using SharpDX;

namespace Spark.Client
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var app = new Game();
            //var app = new EditorApp();
            app.Run();
        }
    }
}