using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squid;

namespace Spark.Editor
{
    public class GuiWindow : Window
    {
        public TitleBar Titlebar { get; private set; }

        private Point FloatSize;
        private bool Docked;
        private bool dragging;

        public GuiWindow()
        {
            Style = "window";
            MaxSize = Point.Zero;
            SnapDistance = 0;
            Padding = new Margin(1);
            FloatSize = new Point(200, 400);
            
            Titlebar = new TitleBar();
            Titlebar.Dock = DockStyle.Top;
            Titlebar.Size = new Squid.Point(122, 28);
            Titlebar.Margin = new Squid.Margin(0, 0, 0, 1);
            Titlebar.Cursor = Cursors.Move;
            Titlebar.Style = "frame";

            Titlebar.Button.MouseClick += Button_MouseClick;

            //Titlebar.MouseDown += delegate(Control sender, MouseEventArgs args)
            //{
            //    //Size = FloatSize; 
            //    //Dock = DockStyle.None; 
            //    StartDrag();
            //};

            Titlebar.MouseDrag += delegate(Control sender, MouseEventArgs args)
            {
                //Size = FloatSize; 
                //Dock = DockStyle.None; 
                StartDrag();

                if (Dock != DockStyle.None)
                {
                    Point p = Position;
                    Dock = DockStyle.None;
                    Position = p;
                }

                //BringToFront();
            };

            Titlebar.MouseUp += delegate(Control sender, MouseEventArgs args)
            {
                StopDrag();
            };
            
            Controls.Add(Titlebar);
        }

        void Button_MouseClick(Control sender, MouseEventArgs args)
        {
            Close();
        }
    }

    public class TitleBar : Label
    {
        public Button Button { get; private set; }

        public TitleBar()
        {
            Button = new Button();
            Button.Size = new Point(24, 16);
            Button.Style = "close";
            Button.Tooltip = "Close Window";
            Button.Dock = DockStyle.Right;
            Button.Margin = new Margin(0, 2, 2, 2);
            Button.Tint = ColorInt.ARGB(1, 0, 1, 1);
            Elements.Add(Button);
        }
    }
}
