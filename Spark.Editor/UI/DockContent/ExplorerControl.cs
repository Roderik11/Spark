using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squid;
using Spark;
using System.Reflection;
using System.IO;
using System.Security.Permissions;
using Spark.Graph;

namespace Spark.Editor
{
 
    public class ExplorerControl : Frame
    {
        public class Header : Frame
        {
            private Button button1;
            private Button button2;
            private Label label1;
            private Label label2;

            public Header()
            {
                NoEvents = false;
                Size = new Point(32, 34);
                Padding = new Margin(0, 0, 0, 0);
                Margin = new Margin(0, 0, 0, 1);
                Dock = DockStyle.Top;
                //Style = "category";

                button1 = new Button
                {
                    Margin = new Margin(0, 0, 1, 0),
                    Style = "header",
                    Dock = DockStyle.Left,
                    Size = new Point(26, 26),
                };

                button2 = new Button
                {
                    Margin = new Margin(0, 0, 1, 0),
                    Style = "header",
                    Dock = DockStyle.Left,
                    Size = new Point(26, 26),
                };

                label1 = new Label
                {
                    NoEvents = true,
                    Margin = new Margin(0, 0, 1, 0),
                    Size = new Point(200, 26),
                    Style = "header",
                    Dock = DockStyle.Left,
                    Text = "Name"
                };

                label2 = new Label
                {
                    NoEvents = true,
                    Margin = new Margin(0, 0, 0, 0),
                    Size = new Point(220, 26),
                    Style = "header",
                    Dock = DockStyle.Fill,
                    Text = "Type"
                }; ;

                Controls.Add(button1);
                Controls.Add(button2);
                Controls.Add(label1);
                Controls.Add(label2);
            }

            protected override void OnStateChanged()
            {
                label1.State = State;
                label2.State = State;
                button1.State = State;
                button2.State = State;
            }
        }

        public class ExplorerNode : Button
        {
            public ImageControl Button1 { get; private set; }
            public ImageControl Button2 { get; private set; }
            public ImageControl Foldout { get; private set; }
            public Label Label1 { get; private set; }
            public Label Label2 { get; private set; }
            public Control IndentFrame { get; private set; }

            public int Indent;

            public event Action<int, Entity> ExpandedChanged;

            public int NodeIndex {get; private set;}
            public Entity Entity { get; private set; }

            public ExplorerNode()
            {
                Size = new Point(100, 28);
                Dock = DockStyle.Top;
                Style= "";
                
                IndentFrame = new Control()
                {
                    Style = "indent18",
                    NoEvents = true,
                    Dock = DockStyle.Left,
                };

                Button1 = new ImageControl
                {
                    Style = "item",
                    Size = new Point(27, 26),
                    Dock = DockStyle.Left,
                    Tiling = TextureMode.Center,
                    Color = ColorInt.ARGB(1f, .5f, .5f, .5f)
                };

                Button2 = new ImageControl
                {
                    Style = "item",
                    Size = new Point(27, 26),
                    Dock = DockStyle.Left,
                    Tiling = TextureMode.Center,
                    Color = ColorInt.ARGB(1f, .5f, .5f, .5f)
                };

                Foldout = new ImageControl
                {
                    Style = "item",
                    NoEvents = true,
                    Size = new Point(26, 26),
                    Dock = DockStyle.Left,
                    Enabled = false,
                    Tiling = TextureMode.Center,
                    Color = ColorInt.ARGB(1f, .5f, .5f, .5f)
                };

                Label1 = new Button
                {
                    Style = "item",
                    Size = new Point(200 - 27, 20),
                    Dock = DockStyle.Left,
                    NoEvents = true
                };

                Label2 = new Button
                {
                    Style = "item",
                    Size = new Point(20, 20),
                    Dock = DockStyle.Fill,
                    NoEvents = true
                };

                Elements.Add(Button1);
                Elements.Add(Button2);
                Elements.Add(IndentFrame);
                Elements.Add(Foldout);
                Elements.Add(Label1);
                Elements.Add(Label2);

                Foldout.MouseClick += Foldout_MouseClick;
            }

            public void Bind(Entity entity, int index)
            {
                Entity = entity;
                NodeIndex = index;
                Tag = entity;

                int childCOunt = entity.Transform.GetChildCount();

                Foldout.Enabled = childCOunt > 0;
                Foldout.NoEvents = childCOunt == 0;
                Foldout.Texture = childCOunt > 0 ? "nav_right.dds" : "";
                Foldout.Texture = entity.Expanded ? "nav_down.dds" : Foldout.Texture;
                Selected = Selector.SelectedEntity == entity;

                Label1.Text = entity.Name;
                IndentFrame.Size = new Point(Indent * entity.Transform.Depth, IndentFrame.Size.y);
                Label1.Size = new Point((200 - 27) - Indent * entity.Transform.Depth, Label1.Size.y);
                Label2.Text = string.Empty;
                if (Selected) Focus();

                Component lastComponent = null;
                foreach (var comp in entity.GetComponents())
                {
                    if (lastComponent is Transform)
                    {
                        Label2.Text = comp.GetType().Name;
                        break;
                    }
                    lastComponent = comp;
                }
            }

            protected override void OnStateChanged()
            {
                IndentFrame.State = State;
                Foldout.State = State;
                Label1.State = State;
                Label2.State = State;
                Button1.State = State;
                Button2.State = State;
            }

            void Foldout_MouseClick(Control sender, MouseEventArgs args)
            {
                if (args.Button > 0) return;
                ExpandedChanged?.Invoke(NodeIndex, Entity);
            }
        }

        private VirtualList VirtualList;
        private Frame toolbar;
        private SearchBox searchbox;
        private Frame header;
        private bool selectionSender;

        private List<Entity> entities = new List<Entity>();
        private List<Entity> bindList;
        private Label footerLabel;

        public ExplorerControl()
        {
            Size = new Point(340, 200);

            toolbar = new Frame
            {
                Style = "frame",
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

            header = new Header();

            footerLabel = new Label
            {
                Style = "header",
                Size = new Point(16, 28),
                Dock = DockStyle.Bottom,
                Margin = new Margin(0, 1, 0, 0),
                Text = "100 Entities"
            };

            searchbox.TextChanged += Searchbox_TextChanged;

            VirtualList = new VirtualList();
            VirtualList.Dock = DockStyle.Fill;
            VirtualList.Scrollbar.ButtonDown.Visible = false;
            VirtualList.Scrollbar.ButtonUp.Visible = false;
            VirtualList.Scrollbar.Slider.Ease = false;
            VirtualList.Scrollbar.Slider.MinHandleSize = 64;
            VirtualList.CreateItem = CreateNode;
            VirtualList.BindItem = BindNode;
            //VirtualList.Scrollbar.Ease = true;

            toolbar.Controls.Add(searchbox);
            Controls.Add(toolbar);
            Controls.Add(header);
            Controls.Add(footerLabel);
            Controls.Add(VirtualList);

            MessageDispatcher.AddListener(Msg.RefreshExplorer, (msg) => { Refresh(); });
            MessageDispatcher.AddListener(Msg.SelectionChanged, (msg) =>
            {
                if (selectionSender) return;

                var entity = msg.Data as Entity;
                if(entity != null)
                    ExpandTo(entity);

                VirtualList.Refresh();
            });

            bindList = entities;
        }

        private void ExpandTo(Entity entity)
        {
            var child = entity;
            var root = entity;

            while (root.Transform.Parent != null)
                root = root.Transform.Parent.Entity;

            int index;

            if (root.Expanded)
            {
                var remove = new List<Entity>();
                index = entities.IndexOf(root);
                FindExpandedChildren(root, remove);
                entities.RemoveRange(index + 1, remove.Count);
            }

            while (entity.Transform.Parent != null)
            {
                entity = entity.Transform.Parent.Entity;
                entity.Expanded = true;
            }

            if (root.Expanded)
            {
                var add = new List<Entity>();
                FindExpandedChildren(root, add);
                index = entities.IndexOf(root);
                entities.InsertRange(index + 1, add);
            }

            int childindex = entities.IndexOf(child);

            VirtualList.Refresh();
            //VirtualList.UpdateVirtualList();
            VirtualList.ScrollTo(childindex);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            footerLabel.Text = $"{entities.Count} Entities";
        }

        private void Searchbox_TextChanged(Control sender)
        {
            var str = searchbox.Text;
            bool isempty = string.IsNullOrEmpty(str);
            str = str.ToLower();

            bindList = entities;

            if (!isempty)
            {
                var list = new List<Entity>();

                foreach (Entity e in Entity.Entities)
                {
                    if (e.Hidden) continue; ;
                    if (e.Transform.Parent != null) continue;
                    if (e.Name.ToLower().Contains(str))
                        list.Add(e);
                }
                bindList = list;
            }

            VirtualList.DataSource = bindList;
            VirtualList.Refresh();
        }

        public void Refresh()
        {
            entities.Clear();

            foreach (Entity e in Entity.Entities)
            {
                if (e.Hidden) continue; ;
                if (e.Transform.Parent != null) continue;
                entities.Add(e);

                if (e.Expanded)
                    FindExpandedChildren(e, entities);
            }

            VirtualList.DataSource = bindList;
            VirtualList.Refresh();
            //VirtualList.UpdateVirtualList();
        }

        void FindExpandedChildren(Entity entity, List<Entity> result)
        {
            var childCount = entity.Transform.GetChildCount();
            for (int i = 0; i < childCount; i++)
            {
                var child = entity.Transform.GetChild(i);
                result.Add(child.Entity);

                if (child.Entity.Expanded)
                    FindExpandedChildren(child.Entity, result);
            }
        }

        private void BindNode(Control control, int index)
        {
            var node = control as ExplorerNode;
            var entity = bindList[index];
            node.Bind(entity, index);
        }

        private ExplorerNode CreateNode(int index)
        {
            var entity = bindList[index];

            ExplorerNode node = new ExplorerNode();
            node.Indent = 14;
            node.MouseClick += Node_MouseClick;
            node.ExpandedChanged += Node_ExpandedChanged;
            node.MouseDoubleClick += Node_MouseDoubleClick;
            node.AllowFocus = true;
            node.Bind(entity, index);

            node.KeyDown += (s, e) =>
            {
                var myEntity = (s as ExplorerNode).Entity;
                var myIndex = bindList.IndexOf(myEntity);

                if (e.Key == Keys.RIGHTARROW && !myEntity.Expanded)
                    Node_ExpandedChanged(myIndex, myEntity);

                if (e.Key == Keys.LEFTARROW && myEntity.Expanded)
                    Node_ExpandedChanged(myIndex, myEntity);

                if (e.Key == Keys.DOWNARROW)
                {
                    var nextIndex = Math.Min(myIndex + 1, bindList.Count - 1);
                    var nextDir = bindList[nextIndex];
                    Select(nextDir);
                    VirtualList.ScrollTo(nextIndex);
                }

                if (e.Key == Keys.UPARROW)
                {
                    var prev = Math.Max(myIndex - 1, 0);
                    var nextDir = bindList[prev];
                    Select(nextDir);
                    VirtualList.ScrollTo(prev);
                };
            };

            return node;
        }

        private void Node_MouseClick(Control sender, MouseEventArgs args)
        {
            var node = sender as ExplorerNode;
            Select(node.Entity);
        }

        void Select(Entity entity)
        {
            selectionSender = true;
            Selector.SelectedEntity = entity;
            selectionSender = false;
            VirtualList.Refresh();
        }

        private void Node_ExpandedChanged(int index, Entity entity)
        {
            entity.Expanded = !entity.Expanded;

            var list = new List<Entity>();
            FindExpandedChildren(entity, list);

            if (entity.Expanded)
                entities.InsertRange(index + 1, list);
            else
                entities.RemoveRange(index + 1, list.Count);

            VirtualList.Refresh();
            //VirtualList.UpdateVirtualList();
        }

        private void Node_MouseDoubleClick(Control sender, MouseEventArgs args)
        {
            var node = sender as ExplorerNode;
            var entity = node.Entity;


            MessageDispatcher.Send(Msg.FocusEntity, entity);
        }
    }
}
