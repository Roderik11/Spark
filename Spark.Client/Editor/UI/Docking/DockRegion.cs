using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squid;
using Spark;
using System.Reflection;
using System.IO;


namespace Spark.Client
{
    public class DockRegion : Frame
    {
        private readonly DockGroup dockGroup;

        public DockRegion()
        {
            Style = "window";
            Padding = new Margin(0);

            Size = new Point(320, 300);
            Dock = DockStyle.Fill;

            dockGroup = new DockGroup();
            dockGroup.Dock = DockStyle.Fill;
            Controls.Add(dockGroup);
        }

        public DockGroup DockContent(string name, Control control, DockStyle dock = DockStyle.Fill, bool header = true)
        {
            if (control == null) return null;
            control.Dock = DockStyle.Fill;

            var page = new TabPage();
            page.Button.Text = name;
            page.Button.Size = new Point(128, 26);
            page.Button.Margin = new Margin(0, 0, 1, 0);
            page.Button.Style = "tab";
            page.Button.MouseDrag += Button_MouseDrag;
            page.Button.Tag = page;
            page.Button.GetElements().Add(new Button
            {
                Size = new Point(26, 26),
                Dock = DockStyle.Right,
                Style = "closetab"
            });
            page.Controls.Add(control);
            var result  = dockGroup.DockContent(page, dock);

            if (!header)
                dockGroup.ButtonFrame.Visible = false;

            return result;
        }

        public DockGroup DockContent(DockGroup group, string name, Control control, DockStyle dock = DockStyle.Fill, bool header = true)
        {
            if (control == null) return null;
            control.Dock = DockStyle.Fill;

            var page = new TabPage();
            page.Button.Text = name;
            page.Button.Size = new Point(128, 26);
            page.Button.Margin = new Margin(0, 0, 1, 0);
            page.Button.Style = "tab";
            page.Button.MouseDrag += Button_MouseDrag;
            page.Button.Tag = page;
            page.Button.GetElements().Add(new Button
            {
                Size = new Point(26, 26),
                Dock = DockStyle.Right,
                Style = "closetab"
            });
            page.Size = control.Size;
            page.Controls.Add(control);
            var result = group.DockContent(page, dock);

            if (!header)
                group.ButtonFrame.Visible = false;

            return result;
        }

        private void Button_MouseDrag(Control sender, MouseEventArgs args)
        {
            Button image = new Button();
            image.Style = "button";
            image.Text = ((Button)sender).Text;
            image.Size = sender.Size;
            image.Position = sender.Location;
            image.TextAlign = ((Button)sender).TextAlign;
            image.Tag = sender.Tag;

            sender.DoDragDrop(image);

            MessageDispatcher.Send(Msg.StartDockDrag);
            Desktop.DragDropEnded += Desktop_DragDropEnded;
        }

        private void Desktop_DragDropEnded(Control sender, DragDropEventArgs e)
        {
            Desktop.DragDropEnded -= Desktop_DragDropEnded;
            MessageDispatcher.Send(Msg.EndDockDrag);
        }
    }
}
