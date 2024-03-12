using System;
using System.Collections.Generic;
using Squid;
using SharpDX;
using Point = Squid.Point;
using Spark.Graph;
using System.Runtime.CompilerServices;
using System.Data.Common;
using SharpDX.Direct3D11;

namespace Spark.Client
{
    public class GraphEditor : Frame
    {
        private readonly GraphCanvas CanvasFrame;
        private readonly Frame Toolbar;

        private ScrollPanel scrollPanel;
        private GUIInspector nodeList;

        public NodeGraph Graph;

        private struct SortableNode
        {
            public Type Type;
            public string Category;
        }

        public GraphEditor()
        {
            Dock = DockStyle.Fill;
            Size = new Point(800, 600);
            Position = new Point(300, 300);
            MaxSize = Point.Zero;

            Toolbar = new Frame { Style = "frame", Size = new Point(16, 24), Dock = DockStyle.Top };
            Toolbar.Margin = new Margin(0, 0, 0, 1);
            Controls.Add(Toolbar);

            Button btnSave = new Button();
            btnSave.Text = "Save";
            btnSave.Size = new Point(100, 20);
            btnSave.Dock = DockStyle.Left;
            btnSave.MouseClick += BtnSave_MouseDown;
            btnSave.Style = "button";
            btnSave.Margin = new Squid.Margin(1);
            Toolbar.Controls.Add(btnSave);

            Button btnLoad = new Button();
            btnLoad.Text = "Load";
            btnLoad.Size = new Point(100, 20);
            btnLoad.Dock = DockStyle.Left;
            btnLoad.MouseClick += BtnLoad_MouseDown;
            btnLoad.Style = "button";
            btnLoad.Margin = new Squid.Margin(1);
            Toolbar.Controls.Add(btnLoad);

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.SplitFrame1.Size = new Point(200, 200);
            split.SplitButton.Margin = new Squid.Margin(1, 0, 1, 0);
            split.SplitButton.Size = new Squid.Point(2, 2);
            split.RetainAspect = false;
            Controls.Add(split);

            Graph = new NodeGraph();

            CanvasFrame = new GraphCanvas();
            CanvasFrame.Style = "canvas";
            CanvasFrame.Graph = Graph;

            split.SplitFrame2.Controls.Add(CanvasFrame);

            List<Type> types = Spark.Reflector.GetTypes<Node>();
            types.Sort((a, b) => a.Name.CompareTo(b.Name));


            var obj = new GUIObject(new object());
            nodeList = new GUIInspector(obj);
            scrollPanel = new ScrollPanel();
            scrollPanel.Content.Controls.Add(nodeList);
            split.SplitFrame1.Controls.Add(scrollPanel);

            var sortable = new List<SortableNode>();
            foreach (Type type in types)
            {
                var cat = Reflector.GetAttribute<System.ComponentModel.CategoryAttribute>(type);
                sortable.Add(new SortableNode { Type = type, Category = cat != null ? cat.Category : "Other" });
            }

            sortable.Sort((a, b) =>
            {
                int compare = a.Category.CompareTo(b.Category);
                if (compare != 0) return compare;
                return a.Type.Name.CompareTo(b.Type.Name);
            });

            string categoryName = string.Empty;

            foreach (var item in sortable)
            {
                if (item.Category != categoryName)
                {
                    categoryName = item.Category;
                    var c = nodeList.AddCategory(item.Category);
                    c.Size = new Point(c.Size.x, 24);
                }

                Button btn = new Button();
                btn.Text = item.Type.Name;
                btn.Size = new Point(100, 24);
                btn.Dock = DockStyle.Top;
                btn.MouseDrag += Btn_MouseDrag;
                btn.Tag = item.Type;
                btn.Style = "item";
                btn.Margin = new Squid.Margin(0, 0, 0, 1);
                nodeList.AddControl(btn);
            }
        }

        private void ResetCanvas(NodeGraph graph)
        {
            CanvasFrame.Clear();
            CanvasFrame.SetGraph(graph);

            foreach (var node in Graph.Nodes)
                CanvasFrame.AddNode(node);
        }

        void BtnSave_MouseDown(Control sender, MouseEventArgs args)
        {
            using (System.Windows.Forms.SaveFileDialog dlg = new System.Windows.Forms.SaveFileDialog())
            {
                // dlg.InitialDirectory = Editor.Directories.Content;
                dlg.Filter = "JSON | *.json";

                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var json = JSONSerializer.Serialize(Graph);
                    var text = json.ToText();
                    System.IO.File.WriteAllText(dlg.FileName, text);
                }
            }
        }

        void BtnLoad_MouseDown(Control sender, MouseEventArgs args)
        {
            using (System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog())
            {
                // dlg.InitialDirectory = Editor.Directories.Content;
                dlg.Filter = "JSON | *.json";

                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var text = System.IO.File.ReadAllText(dlg.FileName);
                    var json = new JSON(text);
                    Graph = JSONSerializer.Deserialize(json) as NodeGraph;
                    ResetCanvas(Graph);
                }
            }
        }

        void Btn_MouseDrag(Control sender, MouseEventArgs args)
        {
            Label label = new Label();
            label.Tag = sender.Tag;
            label.Text = ((Button)sender).Text;
            label.Size = sender.Size;
            label.Position = Gui.MousePosition;
            DoDragDrop(label);
        }
    }

    public class GraphCanvas : Window
    {
        private Control DownSource;
        private Point maxSize = new Point(1000000, 1000000);
        private bool isDragging;

        public NodeGraph Graph;

        public GraphCanvas()
        {
            DownSource = null;
            UIScale = 1f;
            Resizable = false;
            AllowDragOut = true;

            Dock = DockStyle.Center;
            Size = maxSize;
            Position = Size / -2;
            DragDrop += Canvas_DragDrop;
            AllowDrop = true;
            NoEvents = false;

            MouseDown += Canvas_MouseDown;
            MouseUp += Canvas_MouseUp;

            Gui.MouseDown += Gui_MouseDown;
            Gui.MouseUp += Gui_MouseUp;
        }

        public void Clear()
        {
            Controls.Clear();
        }

        public void SetGraph(NodeGraph graph)
        {
            Graph = graph;

            UIScale = 1;
            Size = maxSize;
            Position = Size / -2;
        }

        public void AddNode(Node node)
        {
            GraphFrame frame = new GraphFrame(node);
            Controls.Add(frame);
        }

        private void Gui_MouseUp(Control sender, MouseEventArgs args)
        {
            DownSource = null;
        }

        private void Gui_MouseDown(Control sender, MouseEventArgs args)
        {
            if (sender is Plug plug && plug.IsChildOf(this))
                DownSource = plug;
        }
        

        private void Canvas_MouseUp(Control sender, MouseEventArgs args)
        {
            if (args.Button == 2)
            {
                StopDrag();
                isDragging = false;
            }
        }

        private void Canvas_MouseDown(Control sender, MouseEventArgs args)
        {
            if (args.Button == 2)
            {
                StartDrag();
                isDragging = true;
            }
        }

        void Canvas_DragDrop(Control sender, DragDropEventArgs e)
        {
            if (e.DraggedControl == null) return;

            Type type = e.DraggedControl.Tag as Type;
            if (typeof(Node).IsAssignableFrom(type))
            {
                var node = Activator.CreateInstance(type) as Node;
                Graph.AddNode(node);
                
                GraphFrame frame = new GraphFrame(node);
                frame.Position = (Gui.MousePosition - Location) / UIScale;
                Controls.Add(frame);
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (isDragging) return;

            var mouse = Gui.MousePosition;
            var mouseDelta = Math.Sign(Input.MouseWheelDelta);
            var oldScale = UIScale;

            if (Hit(mouse.x, mouse.y) && mouseDelta != 0)
                UIScale += .05f * mouseDelta;

            if (Input.IsKey(System.Windows.Forms.Keys.Oemplus))
                UIScale += .020f;

            if (Input.IsKey(System.Windows.Forms.Keys.OemMinus))
                UIScale -= .020f;

            UIScale = MathUtil.Clamp(UIScale, 0.3f, 2);
            RendererSlimDX.Spritebatch.LineWidth = Math.Max(3, 4 * UIScale);

            if (UIScale == oldScale) return;
            var ratio = UIScale / oldScale;

            if (Dock != DockStyle.None)
            {
                var p = Position;
                var s = Size;
                Dock = DockStyle.None;
                Position = p;
                Size = s;
            }

            var pointInChild = (mouse - Location);

            Size = maxSize * UIScale;
            Position = (mouse - pointInChild * ratio) - Parent.Location;
        }

        protected override void DrawCustom()
        {
            var batch = RendererSlimDX.Spritebatch;

            if (DownSource != null)
            {
                Point a = DownSource.Location + DownSource.Size * UIScale / 2;
                batch.DrawLine(a.x, a.y, Gui.MousePosition.x, Gui.MousePosition.y, new Color4(1, 1, 1, 1));
            }
        }

        protected override void DrawBeforeChildren()
        {
            var batch = RendererSlimDX.Spritebatch;

            foreach (var connection in Graph.Connections)
            {
                var plugA = connection.PortA.Tag as Plug;
                var plugB = connection.PortB.Tag as Plug;

                Point start = plugA.Location + plugA.Size * UIScale / 2;
                Point end = plugB.Location + plugB.Size * UIScale / 2;

                Point offset = new Point(20, 0) * UIScale;
                Point offsetA = start + offset * ((int)plugA.Port.Type * 2 - 1);
                Point offsetB = end + offset * ((int)plugB.Port.Type * 2 - 1);
                batch.DrawLine(start.x, start.y, offsetA.x, offsetA.y, new Color4(.7f, .8f, 1, 1));
                batch.DrawLine(offsetA.x, offsetA.y, offsetB.x, offsetB.y, new Color4(.7f, .8f, 1, 1));
                batch.DrawLine(offsetB.x, offsetB.y, end.x, end.y, new Color4(.7f, .8f, 1, 1));

                //Point last;
                //Point now = start;

                //var count = 64f;
                //for (int i = 0; i < count; i++)
                //{
                //    last = now;
                //    now = Hermite2(start, end, i / count);
                //    batch.DrawLine(last.x, last.y, now.x, now.y, new Color4(.7f, .8f, 1, 1));
                //}

                //batch.DrawLine(now.x, now.y, end.x, end.y, new Color4(.9f, 1, 1, 1));
            }
        }

        private Point Hermite2(Point a, Point b, float step)
        {
            float bend = 64 * UIScale;
            Vector2 v = Vector2.Hermite(new Vector2(a.x, a.y), new Vector2(bend, bend), new Vector2(b.x, b.y), new Vector2(bend, bend), step);
            return new Point((int)v.X, (int)v.Y);
        }
    }

    public enum PlugType
    {
        In, Out
    }

    public class Plug : Button
    {
        public Plug ConnectedTo;
        public PlugType Type;
        public object Owner;
        public GUIProperty Property;
        public Port Port;

        public Plug(Port port, GUIProperty property)
        {
            AllowDrop = true;

            Name = property.Name;

            Port = port;
            Property = property;
            Port.Tag = this;

            DragResponse += Plug_DragResponse;
            MouseDrag += Plug_MouseDrag;
            DragLeave += Plug_DragLeave;
            DragDrop += Plug_DragDrop;

            Selected = port.IsConnected;
        }

        private void Plug_DragDrop(Control sender, DragDropEventArgs e)
        {
            var plug = e.Source as Plug;
            var graph = Port.Graph;
           
            var portA = Port;
            var portB = plug.Port;

            if (!graph.CanConnect(portA, portB))
                return;

            if (portA.ConnectionType == ConnectionType.Single)
            {
                Selected = false;
                graph.ClearConnections(portA);
            }

            if (portB.ConnectionType == ConnectionType.Single)
            {
                plug.Selected = false;
                graph.ClearConnections(portB);
            }

            graph.AddConnection(portA, portB);
            plug.Selected = Selected = true;
        }

        private void Plug_MouseDrag(Control sender, MouseEventArgs args)
        {
            bool inport = Type == PlugType.In;

            Control drag = new Control()
            {
                Style = inport ? "inport" : "outport",
                Tag = this,
                Size = Size,
                NoEvents = true,
                Position = Gui.MousePosition - ClipRect.Size / 2
            };

            DoDragDrop(drag);
        }

        private void Plug_DragLeave(Control sender, DragDropEventArgs e)
        {
            State = ControlState.Default;
        }

        private void Plug_DragResponse(Control sender, DragDropEventArgs e)
        {
            State = ControlState.Selected;
        }
    }

    public class PortControl : Control
    {
        private readonly Label Label;
        private readonly GUIProperty Property;
        private readonly Node Node;
        
        public Plug Plug { get; private set; }

        public PortControl(GUIProperty property, Node node, PlugType type)
        {
            Node = node;
            Property = property;

            Dock = DockStyle.Top;
            Size = new Point(100, 20);
            Dock = DockStyle.Top;
            NoEvents = true;

            bool inport = type == PlugType.In;
            var port = node.GetPort(property.Name);

            Plug = new Plug(port, property)
            {
                Style = inport ? "inport" : "outport",
                Type = type,
                Size = new Point(20, 20),
                Dock = inport ? DockStyle.Left : DockStyle.Right,
                Owner = Node,
                Property = Property
            };

            Label = new Label();
            Label.Size = new Point(20, 20);
            Label.Dock = DockStyle.Fill;
            Label.Text = Property.Name;
            Label.TextAlign = inport ? Alignment.MiddleLeft : Alignment.MiddleRight;
            Label.NoEvents = true;

            Elements.Add(Plug);
            Elements.Add(Label);
        }
    }

    public class GraphFrame : GuiWindow
    {
        public Node Node { get; private set; }
        private PropertyFrame propertyGrid;

        public GraphFrame(Node node)
        {
            Node = node;
            AllowDragOut = true;
            Resizable = true;
            Cursor = Cursors.Move;
            SnapGrid = 8;
          //  Style = "GraphNode";

            MinSize = new Point(168, 80);
            MaxSize = new Point(216, 600);

            Titlebar.Cursor = Cursors.Move;
            Titlebar.Text = Node.GetType().Name;
            Titlebar.Style = "header";
            Titlebar.Button.Visible = false;

            Position = new Point((int)node.Position.X, (int)node.Position.Y);

            propertyGrid = new PropertyFrame
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(propertyGrid);

            var obj = new GUIObject(Node);

            int h = 50;
            foreach (var property in obj.GetProperties())
            {
                var input = property.GetAttribute<InputAttribute>();
                var output = property.GetAttribute<OutputAttribute>();

                if (input != null)
                {
                    PortControl conn = new PortControl(property, Node, PlugType.In);
                    propertyGrid.Controls.Add(conn);
                    h += 26;
                }
                else if (output != null)
                {
                    PortControl conn = new PortControl(property, Node, PlugType.Out);
                    propertyGrid.Controls.Add(conn);
                    h += 26;
                }
                //else
                //{
                //    propertyGrid.AddProperty(info);
                //    h += 26;
                //}
            }

            Size = new Point(136, h);

            MouseDown += (s, e) =>
            {
                Selector.SelectedObject = Node;
                BringToFront();
                StartDrag();
            };

            MouseDrag += (s, e) => StartDrag();

            MouseUp += (s, e) => StopDrag();

            Titlebar.MouseDown += (s, e) =>
            {
                Selector.SelectedObject = Node;
                BringToFront();
            };

            PositionChanged += GraphFrame_PositionChanged;
        }

        private void GraphFrame_PositionChanged(Control sender)
        {
            Node.Position = new Vector2(sender.Position.x, sender.Position.y);
        }
    }
}

