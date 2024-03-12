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
    public class TreeNodeEx : TreeNode
    {
        public ImageControl Button { get; private set; }
        public ImageControl Icon { get; private set; }
        public Label Label { get; private set; }
        public Control IndentFrame { get; private set; }

       public int Indent;

        public TreeNodeEx()
        {
            IndentFrame = new Control()
            {
                NoEvents = true,
                Dock = DockStyle.Left,
            };

            Button = new ImageControl
            {
                NoEvents = true,
                Size = new Point(20, 20),
                Dock = DockStyle.Left,
                Enabled = false,
                Tiling = TextureMode.Center,
                Color = ColorInt.ARGB(1f, .5f, .5f, .5f)
            };

            Icon = new ImageControl
            {
                Size = new Point(16, 20),
                Dock = DockStyle.Left,
                NoEvents = true,
                Tiling = TextureMode.Center,
                Style = "item",
                Texture = "folder.dds",
                Color = ColorInt.ARGB(1f, .5f, .5f, .5f)
            };

            Label = new Button
            {
                Size = new Point(20, 20),
                Dock = DockStyle.Fill,
                NoEvents = true
            };

            Elements.Add(IndentFrame);
            Elements.Add(Button);
            Elements.Add(Icon);
            Elements.Add(Label);

            Button.MouseClick += Button_MouseClick;
            MouseClick += Node_MouseClick;
            Nodes.ItemAdded += Nodes_ItemAdded;
            ExpandedChanged += TreeNodeEx_ExpandedChanged;
        }

        private void TreeNodeEx_ExpandedChanged(Control sender)
        {
            Button.Texture = Expanded ? "nav_down.dds" : "nav_right.dds";
        }

        private void Nodes_ItemAdded(object sender, Squid.ListEventArgs<TreeNode> e)
        {
            Button.Enabled = true;
            Button.NoEvents = false;
            Button.Texture = Nodes.Count > 0 ? "nav_right.dds" : "";
        }

        protected override void OnStateChanged()
        {
            IndentFrame.State = State;
            Button.State = State;
            Label.State = State;
            Icon.State = State;
        }

        protected override void OnDepthChanged()
        {
            IndentFrame.Size = new Point(Indent * NodeDepth, IndentFrame.Size.y);
        }

        void Node_MouseClick(Control sender, MouseEventArgs args)
        {
            if (args.Button > 0) return;

            Selected = true;
        }

        void Button_MouseClick(Control sender, MouseEventArgs args)
        {
            if (args.Button > 0) return;

            Expanded = !Expanded;
        }
    }
}
