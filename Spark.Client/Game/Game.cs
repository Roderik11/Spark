using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spark;
using Squid;
using SharpDX;
using SharpDX.Windows;
using SharpDX.Direct3D11;
using System.Windows.Forms;

namespace Spark.Client
{
    public class Game
    {
        private RenderView View;
        private RenderForm Form;

        public void Run()
        {
            Form = new SharpDX.Windows.RenderForm
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

            Reflector.RegisterAssemblies(System.Reflection.Assembly.GetAssembly(typeof(Game)));
            Engine.Initialize("D:\\Projects\\2024\\ProjectXYZ\\", false);

            View = new RenderView(Form.Handle)
            {
                Pipeline = new DeferredRenderer()
            };

            new GameTestScene();
            MessageDispatcher.Send(Msg.RefreshExplorer);
            Form.WindowState = FormWindowState.Normal;

            Engine.Run(Form);
        }

        void Form_SizeChanged(object sender, EventArgs e)
        {
            View?.Resize();
        }
    }
}
