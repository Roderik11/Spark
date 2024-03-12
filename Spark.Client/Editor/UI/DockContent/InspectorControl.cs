using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squid;
using Spark;
using System.Reflection;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Spark.Client
{
    public class InspectorControl : Frame
    {
        private readonly Frame toolbar;
        private readonly SearchBox searchbox;
        private readonly ScrollPanel scrollpanel;
        private readonly SplitContainer split;
        private Point lastSize = new Point(200, 400);

        public InspectorControl()
        {
            Size = new Point(420, 200);
            Dock = DockStyle.Right;

            split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.RetainAspect = false;
            split.Orientation = Orientation.Vertical;
            split.SplitFrame1.Size = new Point(200, 200);
            split.SplitButton.Size = new Point(4, 4);
            split.SplitButton.Margin = new Margin(1, 0, 1, 0);
            Controls.Add(split);

            toolbar = new Frame
            {
                Style = "colorGrey170",
                Size = new Point(16, 40),
                Dock = DockStyle.Top,
                Margin = new Margin(0, 0, 0, 1)
            };

            searchbox = new SearchBox
            {
                Size = new Point(200, 16),
                Dock = DockStyle.Fill,
                Margin = new Margin(28, 8, 8, 8)
            };

            scrollpanel = new ScrollPanel
            {
                Style = "colorGrey170",
                Dock = DockStyle.Fill
            };

            scrollpanel.Content.Style = "frame";
            scrollpanel.VScroll.Ease = true;

            toolbar.Controls.Add(searchbox);
            split.SplitFrame1.Controls.Add(toolbar);
            split.SplitFrame1.Controls.Add(scrollpanel);
            
            split.SplitFrame2.Visible = false;
            split.SplitButton.Visible = false;
            split.SplitFrame1.Dock = DockStyle.Fill;
                
                
            searchbox.TextChanged += Searchbox_TextChanged;
            MessageDispatcher.AddListener(Msg.SelectionChanged, OnSelectionChanged);
        }

        private void Searchbox_TextChanged(Control sender)
        {
            foreach (var control in scrollpanel.Content.Controls)
            {
                var inspector = control as GUIInspector;
                inspector.FilterBy(searchbox.Text);
            }

            PerformLayout();
        }

        void OnSelectionChanged(Message msg)
        {
            scrollpanel.Scroll(0);
            scrollpanel.Content.Controls.Clear();

            if (msg.Data == null) return;

            GUIObject target = new GUIObject(msg.Data);

            var inspector = GUIInspector.GetInspector(target);
            if (inspector != null)
            {
                scrollpanel.Content.Controls.Add(inspector);
                inspector.PerformLayout();
                scrollpanel.Content.PerformLayout();
                ActivatePreview(inspector);
            }

            if (msg.Data is Entity entity)
            {
                var components = entity.GetComponents();
                foreach(var component in components)
                {
                    var obj = new GUIObject(component);           
                    var insp = GUIInspector.GetInspector(obj);
                    if (insp != null)
                    {
                        scrollpanel.Content.Controls.Add(insp);
                        insp.PerformLayout();
                    }
                }
                scrollpanel.Content.PerformLayout();
            }
        }

        private Control lastPreview;

        private void ActivatePreview(GUIInspector inspector)
        {
            var preview = inspector.GetPreview();

            bool isOpen = split.SplitFrame2.Visible;

            if (preview == null && isOpen)
                lastSize = split.SplitFrame1.Size;

            split.SplitFrame2.Controls.Clear();
            split.SplitFrame2.Visible = preview != null;
            split.SplitButton.Visible = preview != null;
            split.SplitFrame1.Dock = preview == null ? DockStyle.Fill : DockStyle.Top;

            if (preview != null && !isOpen)
                split.SplitFrame1.Size = lastSize;

            if (lastPreview is IPreview iprev)
                iprev.OnDisable();

            if (preview == null)
                return;

            preview.Dock = DockStyle.Fill;
            split.SplitFrame2.Controls.Add(preview);

            if(preview is IPreview prev)
                prev.OnEnable();
        }
    }

}
