using System;
using System.Threading;
using System.Globalization;
using SharpDX.Windows;
using SharpDX;
using Squid;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Spark.Editor
{
    static class Program
    {
        [DllImport("DwmApi")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);

        static RenderView RenderView;
        static RenderForm Form;
        static EditorUI editorUI;


        [STAThread]
        static void Main()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            var color = System.Drawing.ColorTranslator.FromHtml("#222222");

            Form = new SharpDX.Windows.RenderForm
            {
                BackColor = color,
                WindowState = FormWindowState.Minimized,
                Size = new System.Drawing.Size(1680, 1020),
                StartPosition = FormStartPosition.CenterScreen,
                Text = "Ember"
            };

            Form.SizeChanged += Form_SizeChanged;
            Form.HandleCreated += Form_HandleCreated;
            Form.Show();
            //System.Windows.Forms.Cursor.Hide();

            Thread.Sleep(1000);

            Reflector.RegisterAssemblies(System.Reflection.Assembly.GetAssembly(typeof(Program)));
            Engine.Initialize("D:\\Projects\\2024\\ProjectXYZ\\", true);

            RenderView = new RenderView(Form.Handle)
            {
                OnRender = RenderGui
            };

            RenderView.OnViewsChanged += RenderView_OnViewsChanged;

            Gui.Renderer = new RendererSlimDX();
            new EditorTestScene();
            editorUI = new EditorUI();
            MessageDispatcher.Send(Msg.RefreshExplorer);

            Form.WindowState = FormWindowState.Normal;

            Engine.Run(Form);
        }

        static void RenderView_OnViewsChanged()
        {
            RenderView.All.Remove(RenderView);
            RenderView.All.Add(RenderView);
        }

        static void Form_HandleCreated(object sender, EventArgs e)
        {
            if (DwmSetWindowAttribute(Form.Handle, 19, new[] { 1 }, 4) != 0)
                DwmSetWindowAttribute(Form.Handle, 20, new[] { 1 }, 4);
        }

        static void RenderGui(RenderView view)
        {
            Graphics.SetTargets(RenderView.BackBufferTarget);
            Graphics.SetViewport(new ViewportF(0, 0, RenderView.Size.X, RenderView.Size.Y, 0.0f, 1.0f));
            Graphics.SetScissorRectangle(0, 0, (int)RenderView.Size.X, (int)RenderView.Size.Y);
            Graphics.ClearRenderTargetView(RenderView.BackBufferTarget, new Color4(0, 0, 0, 1));

            RenderView.Prepare();
            editorUI.Update();
            editorUI.Draw();
            RenderView.Present();
        }

        static void Form_SizeChanged(object sender, EventArgs e)
        {
            if (Form.WindowState == FormWindowState.Minimized) return;
            RenderView?.Resize();
        }
    }
}