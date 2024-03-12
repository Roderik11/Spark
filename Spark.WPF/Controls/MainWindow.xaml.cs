using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AvalonDock;
using System.Windows.Interop;
using Spark;
using System.Diagnostics;
using System.Windows.Threading;
using System.ComponentModel;

namespace ED8000
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TimeSpan last;
        private RenderView view;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Reflector.RegisterAssemblies(System.Reflection.Assembly.GetAssembly(typeof(Editor)));

            Engine.Initialize("..\\..\\ProjectXYZ\\", true);

            view = new RenderView(800, 600);
            view.Pipeline = new DeferredRenderer();

            dXRenderControl1.SetBackBuffer(view.BackBuffer);
            dXRenderControl1.SizeChanged += dXRenderControl1_SizeChanged;
            dXRenderControl1.Tag = view;

            ComponentDispatcher.ThreadFilterMessage += ComponentDispatcher_ThreadFilterMessage;
            CompositionTarget.Rendering += CompositionTarget_Rendering;

            Editor.LoadScene();
        }

        void ComponentDispatcher_ThreadFilterMessage(ref MSG msg, ref bool handled)
        {
            // SharpDX.RawInput.Device.HandleMessage(msg.lParam);
        }

        void dXRenderControl1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            int w = (int)e.NewSize.Width;
            int h = (int)e.NewSize.Height;

            view.Resize(w, h);
        }

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            RenderingEventArgs args = (RenderingEventArgs)e;
          
            if (args.RenderingTime == last)
                return;

            last = args.RenderingTime;

            Engine.Tick();

            dXRenderControl1.SetBackBuffer(view.BackBuffer);
            dXRenderControl1.UpdateSurface();
        }

        private void OnShowOutput(object sender, RoutedEventArgs e)
        {
            winOutput.Show();
        }

        private void OnShowErrors(object sender, RoutedEventArgs e)
        {
            winErrors.Show();
        }

        private void OnShowInspector(object sender, RoutedEventArgs e)
        {
            winInspector.Show();
        }

        private void OnShowAssets(object sender, RoutedEventArgs e)
        {
            winAssets.Show();
        }

        private void OnShowExplorer(object sender, RoutedEventArgs e)
        {
            winExplorer.Show();
        }
    }
}
