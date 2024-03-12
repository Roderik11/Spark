using System;
using System.Threading;
using System.Globalization;
using System.Windows.Forms;
using Spark;
using Squid;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Windows;

namespace Spark.Client
{
    static class Program
    {
        static RenderView View;
        static RenderForm Form;


        [STAThread]
        static void Main()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Form = new RenderForm
            {
                //IsFullscreen = true,
                BackColor = System.Drawing.ColorTranslator.FromHtml("#222222"),
                WindowState = FormWindowState.Minimized,
                Size = new System.Drawing.Size(1680, 1020),
                StartPosition = FormStartPosition.CenterScreen,
                Text = "SparkEditor"
            };

            Form.SizeChanged += Form_SizeChanged;
            Form.Show();

            Reflector.RegisterAssemblies(System.Reflection.Assembly.GetAssembly(typeof(GameTestScene)));
            Engine.Initialize("D:\\Projects\\2024\\ProjectXYZ\\", false);

            View = new RenderView(Form.Handle)
            {
                Pipeline = new DeferredRenderer()
            };

            new GameTestScene();

            Form.WindowState = FormWindowState.Normal;

            Engine.Run(Form);
        }
        static void Form_SizeChanged(object sender, EventArgs e)
        {
            View?.Resize();
        }
    }
}