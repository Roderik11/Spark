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
    public class DockGroup : TabControl
    {
        private DockIndicator dockIndicator;

        public DockGroup()
        {
            dockIndicator = new DockIndicator { Dock = DockStyle.Fill, Margin = new Margin(0, 32, 0, 0) };
            dockIndicator.OnDockContent += DockIndicator_OnDockContent;

            ButtonFrame.Size = new Point(100, 32);
            ButtonFrame.AllowDrop = true;
            ButtonFrame.NoEvents = false;
            ButtonFrame.DragEnter += ButtonFrame_DragEnter;
            ButtonFrame.DragLeave += ButtonFrame_DragLeave;
            ButtonFrame.DragResponse += ButtonFrame_DragResponse;
            ButtonFrame.DragDrop += ButtonFrame_DragDrop;

            MessageDispatcher.AddListener(Msg.StartDockDrag, OnStartDockDrag);
            MessageDispatcher.AddListener(Msg.EndDockDrag, OnEndDockDrag);
        }

        bool IsDragValid(DragDropEventArgs e)
        {
            if (!(e.Source.Tag is TabPage tabPage)) return false;
            if (!(tabPage.Tag is DockGroup dockgroup)) return false;
            if (dockgroup == this && TabPages.Count == 1) return false;
            return true;
        }

        private void DockIndicator_OnDockContent(Control sender, DragDropEventArgs e)
        {
            if (!(e.Source.Tag is TabPage tabPage)) return;
            if (!(tabPage.Tag is DockGroup dockgroup)) return;
            if (dockgroup == this && TabPages.Count == 1) return;

            dockgroup.RemoveContent(tabPage);
            DockContent(tabPage, sender.Dock);
        }

        void OnStartDockDrag(Message msg)
        {
            GetElements().Add(dockIndicator);
        }

        void OnEndDockDrag(Message msg)
        {
            GetElements().Remove(dockIndicator);
        }

        public void RemoveContent(Control control)
        {
            TabPages.Remove(control as TabPage);
            SelectedTab = null;

            if (TabPages.Count > 0)
                SelectedTab = TabPages[0];
            else
            {
                if (Parent is DockRegion)
                {
                }
                else
                {
                    var split = Parent.Parent as SplitContainer;
                    var splitparent = split.Parent;

                    var control1 = split.SplitFrame1.Controls[0];
                    var control2 = split.SplitFrame2.Controls[0];

                    if (control2 == this)
                    {
                        control2 = control1;
                        control1 = this;
                    }

                    split.Parent = null;
                    split = null;

                    control2.Parent = splitparent;
                    Parent = null;
                }
            }
        }

        public DockGroup DockContent(Control control, DockStyle dock)
        {
            if (dock == DockStyle.Fill)
            {
                control.Tag = this;
                TabPages.Add(control as TabPage);
                return this;
            }

            var parent = Parent;
            Parent = null;

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
               
            };

            //split.SplitButton.Style = "window";

            var dockgroup = new DockGroup();
            dockgroup.Dock = DockStyle.Fill;
            dockgroup.ButtonFrame.Size = new Point(100, 32);
            dockgroup.TabPages.Add(control as TabPage);
            control.Tag = dockgroup;

            switch (dock)
            {
                case DockStyle.Top:

                    split.SplitFrame1.Controls.Add(dockgroup);
                    split.SplitFrame2.Controls.Add(this);
                    split.SplitFrame1.Size = new Point(Size.x, Size.y / 2);
                    split.SplitFrame2.Size = new Point(Size.x, Size.y / 2);
                    split.SplitButton.Size = new Point(Size.x, 4);
                    break;

                case DockStyle.Bottom:

                    split.SplitFrame1.Controls.Add(this);
                    split.SplitFrame2.Controls.Add(dockgroup);
                    split.SplitFrame1.Size = new Point(Size.x, Size.y / 2);
                    split.SplitFrame2.Size = new Point(Size.x, Size.y / 2);
                    split.SplitButton.Size = new Point(Size.x, 4);
                    split.AspectRatio = .8f;

                    break;

                case DockStyle.Left:

                    split.Orientation = Orientation.Horizontal;
                    split.SplitFrame1.Controls.Add(dockgroup);
                    split.SplitFrame2.Controls.Add(this);
                    split.SplitFrame1.Size = new Point(Size.x / 2, Size.y);
                    split.SplitFrame2.Size = new Point(Size.x / 2, Size.y);
                    split.SplitButton.Size = new Point(4, Size.y);

                    break;

                case DockStyle.Right:

                    split.Orientation = Orientation.Horizontal;
                    split.SplitFrame1.Controls.Add(this);
                    split.SplitFrame2.Controls.Add(dockgroup);
                    split.SplitFrame1.Size = new Point(Size.x - control.Size.x, Size.y);
                    split.SplitFrame2.Size = new Point(control.Size.x, Size.y);
                    split.SplitButton.Size = new Point(4, Size.y);
                    split.AspectRatio = .8f;
                    break;
            }

            split.Parent = parent;

            return dockgroup;
        }

        private void ButtonFrame_DragDrop(Control sender, DragDropEventArgs e)
        {
            if (!IsDragValid(e)) return;
            if (!(e.Source.Tag is TabPage tabPage)) return;
            if (!(tabPage.Tag is DockGroup dockgroup)) return;

            dockgroup.RemoveContent(tabPage);
            DockContent(tabPage, DockStyle.Fill);
        }

        private void ButtonFrame_DragLeave(Control sender, DragDropEventArgs e)
        {
            if (!IsDragValid(e)) return;
            ButtonFrame.Style = "";
        }

        private void ButtonFrame_DragEnter(Control sender, DragDropEventArgs e)
        {
            if (!IsDragValid(e)) return;
            ButtonFrame.Style = "button";
        }

        private void ButtonFrame_DragResponse(Control sender, DragDropEventArgs e)
        {
            if (!IsDragValid(e)) return;
            ButtonFrame.State = ControlState.Selected;
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
