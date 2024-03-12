using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squid;
using Spark;
using System.Reflection;
using System.IO;


namespace Spark.Editor
{
    public class DockIndicator : Frame
    {
        private Frame left;
        private Frame right;
        private Frame top;
        private Frame bottom;

        public event Action<Control, DragDropEventArgs> OnDockContent;

        public DockIndicator()
        {
            left = AddRegion(DockStyle.Left);
            right = AddRegion(DockStyle.Right);
            top = AddRegion(DockStyle.Top);
            top.Margin = new Margin(0, 24, 0, 0);
            bottom = AddRegion(DockStyle.Bottom);
        }

        Frame AddRegion(DockStyle dock)
        {
            var frame = new Frame
            {
                NoEvents = false,
                Dock = dock,
                AllowDrop = true
            };

            Controls.Add(frame);

            frame.DragEnter += Frame_DragEnter;
            frame.DragLeave += Frame_DragLeave;
            frame.DragDrop += Frame_DragDrop;

            return frame;
        }

        private void Frame_DragDrop(Control sender, DragDropEventArgs e)
        {
            if (!IsDragValid(e)) return;

            // dragged thing can only be dockable content
            OnDockContent?.Invoke(sender, e);
        }

        private void Frame_DragLeave(Control sender, DragDropEventArgs e)
        {
            if (!IsDragValid(e)) return;

            sender.Style = "";

            if (sender == top)
                top.Margin = new Margin(0, 24, 0, 0);
        }

        bool IsDragValid(DragDropEventArgs e)
        {
            if (!(e.Source.Tag is TabPage tabPage)) return false;
            if (!(tabPage.Tag is DockGroup dockgroup)) return false;
            if (dockgroup == Parent && dockgroup.TabPages.Count == 1) return false;
            return true;
        }

        private void Frame_DragEnter(Control sender, DragDropEventArgs e)
        {
            if(!IsDragValid(e)) return;

            sender.BringToBack();
            sender.Style = "category";

            if (sender == top)
                top.Margin = new Margin(0, 0, 0, 0);
        }

        protected override void OnUpdate()
        {
            left.Size = new Point(Size.x / 4, left.Size.y);
            right.Size = new Point(Size.x / 4, right.Size.y);
            top.Size = new Point(top.Size.x, Size.y / 4);
            bottom.Size = new Point(bottom.Size.x, Size.y / 4);
        }
    }
}
