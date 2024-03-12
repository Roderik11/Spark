using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.Globalization;
using Spark;
using System.IO;
//using SharpSvn;

namespace Spark.Editor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
         
            LaunchForm launch = new LaunchForm();

            if (launch.ShowDialog() == DialogResult.OK)
            {
                launch.Dispose();

                Reflector.RegisterAssemblies(System.Reflection.Assembly.GetAssembly(typeof(EditorCamera)));

                EditorForm form = new EditorForm();
                form.Show();
            }
        }
    }
}
