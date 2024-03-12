using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Spark.Windows;
using Spark;

namespace Spark.Editor
{
    public partial class Viewport : DockContent
    {
        public RenderView View;
        public EditorCamera Camera;

        public Viewport()
        {
            InitializeComponent();

            SizeChanged += Viewport_SizeChanged;
            HandleCreated += Viewport_HandleCreated;
        }

        void Viewport_SizeChanged(object sender, EventArgs e)
        {
           // if (Width <= 0 || Height <= 0) return;

            if (View != null)
                View.Resize();
        }

        void Viewport_HandleCreated(object sender, EventArgs e)
        {
            if (View != null)
                View.Recreate(Handle);
        }

        private void Viewport_MouseDown(object sender, MouseEventArgs e)
        {
            Parent.Focus();
        }
    }
}
