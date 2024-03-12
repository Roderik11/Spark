//using System;
//using System.Collections.Generic;
//using Squid;
//using System.ComponentModel;
//using SharpDX;
//using Spark;
//using Spark.Noise;
//using SharpDX.Direct3D11;
//using Point = Squid.Point;
//using Spark.Noise.Operator;

//namespace Spark.Client
//{
//    public class NoiseDiagram
//    {
//        public List<DiagramElement> Elements = new List<DiagramElement>();
//    }

//    public class DiagramElement
//    {
//        public Point Position;
//        public Point Size;
//        public object Module;
//    }

//    public class NoiseEditor : DockWindow
//    {
//        private Canvas CanvasFrame;
//        private Frame toolbar;

//        public NoiseEditor()
//        {
//            Dock = DockStyle.Fill;
//            Size = new Point(800, 600);
//            Position = new Point(300, 300);
//            MaxSize = Point.Zero;

//            toolbar = new Frame { Style = "frame", Size = new Point(16, 20), Dock = DockStyle.Top };
//            toolbar.Margin = new Margin(0, 0, 0, 1);
//            Controls.Add(toolbar);

//            Button btnSave = new Button();
//            btnSave.Text = "Save";
//            btnSave.Size = new Point(100, 20);
//            btnSave.Dock = DockStyle.Left;
//            btnSave.MouseDown += btnSave_MouseDown;
//            btnSave.Style = "button";
//            btnSave.Margin = new Squid.Margin(1);
//            toolbar.Controls.Add(btnSave);

//            Button btnLoad = new Button();
//            btnLoad.Text = "Load";
//            btnLoad.Size = new Point(100, 20);
//            btnLoad.Dock = DockStyle.Left;
//            btnLoad.MouseDown += btnLoad_MouseDown;
//            btnLoad.Style = "button";
//            btnLoad.Margin = new Squid.Margin(1);
//            toolbar.Controls.Add(btnLoad);

//            SplitContainer split = new SplitContainer();
//            split.Dock = DockStyle.Fill;
//            split.SplitFrame1.Size = new Point(200, 200);
//            split.SplitButton.Margin = new Squid.Margin(1, 0, 1, 0);
//            split.SplitButton.Size = new Squid.Point(2, 2);
//            split.RetainAspect = false;
//            Controls.Add(split);

//            CanvasFrame = new Canvas();
//            CanvasFrame.Style = "canvas";
//            split.SplitFrame2.Controls.Add(CanvasFrame);

//            List<Type> types = Spark.Reflector.GetTypes<ModuleBase>();
//            types.Sort((a, b) => a.Name.CompareTo(b.Name));

//            foreach (Type type in types)
//            {
//                Button btn = new Button();
//                btn.Text = type.Name;
//                btn.Size = new Point(100, 20);
//                btn.Dock = DockStyle.Top;
//                btn.MouseDown += btn_MouseDown;
//                btn.Tag = type;
//                btn.Style = "item";
//                btn.Margin = new Squid.Margin(0, 0, 0, 1);
//                split.SplitFrame1.Controls.Add(btn);
//            }

//            Button btn2 = new Button();
//            btn2.Text = "Noisemap";
//            btn2.Size = new Point(100, 20);
//            btn2.Dock = DockStyle.Top;
//            btn2.MouseDown += btn_MouseDown;
//            btn2.Tag = typeof(Noise2D);
//            btn2.Style = "item";
//            btn2.Margin = new Squid.Margin(0, 0, 0, 1);
//            split.SplitFrame1.Controls.Add(btn2);
//        }

//        void btnSave_MouseDown(Control sender, MouseEventArgs args)
//        {
//            using (System.Windows.Forms.SaveFileDialog dlg = new System.Windows.Forms.SaveFileDialog())
//            {
//               // dlg.InitialDirectory = Editor.Directories.Content;
//                dlg.Filter = "XML | *.xml";

//                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
//                {
//                    Save(dlg.FileName);
//                }
//            }
//        }

//        void btnLoad_MouseDown(Control sender, MouseEventArgs args)
//        {
//            using (System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog())
//            {
//               // dlg.InitialDirectory = Editor.Directories.Content;
//                dlg.Filter = "XML | *.xml";

//                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
//                {
//                    Load(dlg.FileName);
//                }
//            }
//        }

//        void btn_MouseDown(Control sender, MouseEventArgs args)
//        {
//            Label label = new Label();
//            label.Tag = sender.Tag;
//            label.Text = ((Button)sender).Text;
//            label.Size = sender.Size;
//            label.Position = Gui.MousePosition;
//            DoDragDrop(label);
//        }

//        private void Save(string filename)
//        {
//            NoiseDiagram d = new NoiseDiagram();

//            foreach (Control control in CanvasFrame.Controls)
//            {
//                if (control is ModuleFrame)
//                {
//                    ModuleFrame frame = control as ModuleFrame;
//                    d.Elements.Add(new DiagramElement { Module = frame.Module, Position = new Point(control.Position.x, control.Position.y), Size = new Point(control.Size.x, control.Size.y) });
//                }
//                if (control is NoiseFrame)
//                {
//                    NoiseFrame noise = control as NoiseFrame;
//                    d.Elements.Add(new DiagramElement { Module = noise.Module, Position = new Point(control.Position.x, control.Position.y), Size = new Point(control.Size.x, control.Size.y) });
//                }
//            }

//            Spark.ObjectGraph g = new Spark.ObjectGraph();
//            string data = g.Serialize(d);
//            //var json = JSONSerializer.Serialize(d);
//            //var data = json.ToText();
//            System.IO.File.WriteAllText(filename, data);
//        }

//        private void Load(string filename)
//        {
//            NoiseDiagram diagram = new Spark.ObjectGraph().Deserialize<NoiseDiagram>(filename);

//            CanvasFrame.Controls.Clear();
//            // ClearAllPorts();

//            foreach (DiagramElement element in diagram.Elements)
//            {
//                if (element.Module is Noise2D)
//                {
//                    NoiseFrame frame = new NoiseFrame(element.Module as Noise2D);
//                    frame.Position = new Point(element.Position.x, element.Position.y);
//                    frame.Size = new Point(element.Size.x, element.Size.y);

//                    CanvasFrame.Controls.Add(frame);
//                }
//                else if (element.Module is ModuleBase)
//                {
//                    ModuleFrame frame = new ModuleFrame(element.Module as ModuleBase);
//                    frame.Position = new Point(element.Position.x, element.Position.y);
//                    frame.Size = new Point(element.Size.x, element.Size.y);
                   
//                    CanvasFrame.Controls.Add(frame);
//                }
//            }

//            CanvasFrame.UpdatePorts();
//        }
//    }

//    public class Canvas : Window
//    {
//        private Control DownSource;
//        private readonly List<Plug> ConnectedPorts = new List<Plug>();
//        private Point maxSize = new Point(1000000, 1000000);

//        public void RegisterPort(Plug p)
//        {
//            if (!ConnectedPorts.Contains(p))
//                ConnectedPorts.Add(p);
//        }

//        public void ClearPort(Plug p)
//        {
//            ConnectedPorts.Remove(p);
//        }

//        public void ClearAllPorts()
//        {
//            ConnectedPorts.Clear();
//        }

//        public Canvas()
//        {
//            ConnectedPorts = new List<Plug>();
//            DownSource = null;
//            UIScale = 1f;
//            Resizable = false;
//            AllowDragOut = true;

//            Dock = DockStyle.Center;
//            Size = maxSize;
//            Position = Size / -2;
//            DragDrop += Canvas_DragDrop;
//            AllowDrop = true;
//            NoEvents = false;

//            MouseDown += Canvas_MouseDown;
//            MouseUp += Canvas_MouseUp;

//            Gui.MouseDown += Gui_MouseDown;
//            Gui.MouseUp += Gui_MouseUp;
//        }

//        private void Gui_MouseUp(Control sender, MouseEventArgs args)
//        {
//            DownSource = null;

//            if (sender is Plug plug && plug.IsChildOf(this))
//            {
//                Control hit = Desktop.GetControlAt(Gui.MousePosition.x, Gui.MousePosition.y);
//                if (!(hit is Plug over)) return;
//                if (over.Type == plug.Type) return;

//                if (over.ConnectedTo != null)
//                {
//                    over.ConnectedTo.ConnectedTo = null;
//                    over.ConnectedTo = null;

//                    ClearPort(over);

//                    if (over.ConnectedTo != null)
//                        ClearPort(over.ConnectedTo);
//                }

//                plug.ConnectedTo = over;
//                over.ConnectedTo = plug;

//                if (plug.Type == PlugType.Out)
//                {
//                    plug.ConnectedTo.Property.SetValue(plug.Owner);
//                    RegisterPort(plug);
//                }
//                else
//                {
//                    plug.Property.SetValue(over.Owner);
//                    RegisterPort(over);
//                }
//            }
//        }

//        private void Gui_MouseDown(Control sender, MouseEventArgs args)
//        {
//            if (sender is Plug plug && plug.IsChildOf(this))
//            {
//                ClearPort(plug);

//                if (plug.ConnectedTo != null)
//                    ClearPort(plug.ConnectedTo);

//                //if (Type == PlugType.Out)
//                //    ClearPort(this);
//                //else if (ConnectedTo != null)
//                //    ClearPort(ConnectedTo);

//                if (plug.ConnectedTo != null)
//                    plug.ConnectedTo.ConnectedTo = null;

//                plug.ConnectedTo = null;
//                DownSource = plug;
//            }
//        }

//        private void Canvas_MouseUp(Control sender, MouseEventArgs args)
//        {
//            if(args.Button == 2)
//                StopDrag();
//        }

//        private void Canvas_MouseDown(Control sender, MouseEventArgs args)
//        {
//            if(args.Button == 2)
//                StartDrag();
//        }

//        void Canvas_DragDrop(Control sender, DragDropEventArgs e)
//        {
//            if (e.DraggedControl == null) return;

//            Type type = e.DraggedControl.Tag as Type;
//            if (typeof(ModuleBase).IsAssignableFrom(type))
//            {
//                ModuleBase module = Activator.CreateInstance(type) as ModuleBase;
//                ModuleFrame frame = new ModuleFrame(module);
//                frame.Position = (Gui.MousePosition - Location) / UIScale;
//                Controls.Add(frame);
//            }
//            else if (type.IsAssignableFrom(typeof(Noise2D)))
//            {
//                NoiseFrame frame = new NoiseFrame();
//                frame.Position = (Gui.MousePosition - Location) / UIScale;
//                Controls.Add(frame);
//            }
//        }

//        protected override void OnUpdate()
//        {
//            base.OnUpdate();

//            var mouse = Gui.MousePosition;
//            var oldScale = UIScale;
//            var oldSize = Size;

//            var mouseDelta = Math.Sign(Input.MouseWheelDelta);

//            if (Hit(mouse.x, mouse.y) && mouseDelta != 0)
//                UIScale += .05f * mouseDelta;

//            if (Input.IsKey(System.Windows.Forms.Keys.Oemplus))
//                UIScale += .020f;

//            if (Input.IsKey(System.Windows.Forms.Keys.OemMinus))
//                UIScale -= .020f;

//            UIScale = MathUtil.Clamp(UIScale, 0.3f, 2);

//            if (UIScale != oldScale)
//            {
//                if (Dock != DockStyle.None)
//                {
//                    var p = Position;
//                    var s = Size;
//                    Dock = DockStyle.None;
//                    Position = p;
//                    Size = s;
//                }

//                var pointInChild = (mouse - Location);
//                var ratio = UIScale / oldScale;

//                Size = maxSize * UIScale;
//                Position = (mouse - pointInChild * ratio) - Parent.Location;
//            }
//        }

//        protected override void DrawBeforeChildren()
//        {
//            var batch = RendererSlimDX.Spritebatch;

//            if (DownSource != null)
//            {
//                Point a = DownSource.Location + DownSource.Size / 2;
//                //a *= UIScale;

//                batch.DrawLine(a.x, a.y, (int)Gui.MousePosition.x, (int)Gui.MousePosition.y, new Color4(1, 1, 1, 1));
//            }

//            foreach (Plug port in ConnectedPorts)
//            {
//                if (port.ConnectedTo == null) continue;

//                Point start = port.Location + port.Size / 2;
//                Point end = port.ConnectedTo.Location + port.Size / 2;

//                //start *= UIScale;
//                //end *= UIScale;

//                Point last;
//                Point now = start;

//                for (int i = 0; i < 32; i++)
//                {
//                    last = now;
//                    now = Hermite2(start, end, i / 32f);
//                    batch.DrawLine(last.x, last.y, now.x, now.y, new Color4(1, 1, 1, 1));
//                }

//                batch.DrawLine(now.x, now.y, end.x, end.y, new Color4(1, 1, 1, 1));
//            }
//        }

//        private Point Hermite2(Point a, Point b, float step)
//        {
//            float bend = 64;
          
//            Vector2 v = Vector2.Hermite(new Vector2(a.x, a.y), new Vector2(bend, bend), new Vector2(b.x, b.y), new Vector2(bend, bend), step);
            
//            return new Point((int)v.X, (int)v.Y);
//        }

//        public void UpdatePorts()
//        {
//            foreach (Control control in Controls)
//            {
//                if (!(control is IControlContainer frame)) continue;

//                foreach (Control child in frame.Controls)
//                {
//                    if (!(child is InPort inport)) continue;

//                    object value = inport.Plug.Property.GetValue();

//                    if (value == null) continue;

//                    foreach (Control conn in Controls)
//                    {
//                        if (!(conn is ModuleFrame mod)) continue;

//                        if (mod.Module != value) continue;

//                        foreach (Control child2 in mod.Controls)
//                        {
//                            if (child2 is OutPort outport)
//                            {
//                                inport.Plug.ConnectedTo = outport.Plug;
//                                outport.Plug.ConnectedTo = inport.Plug;
//                                RegisterPort(outport.Plug);
//                                break;
//                            }
//                        }
//                    }
//                }
//            }
//        }
//    }

//    public class ImageFrame : Control
//    {
//        public Texture Texture;

//        protected override void DrawStyle(Style style, float opacity)
//        {
//            // uncomment this to draw the controls style as usual
//            // base.DrawStyle(style, opacity);

//            if (Texture != null)
//            {
//                // we draw a texture on top
//                int color = ColorInt.FromArgb(opacity, -1);
//                Squid.Rectangle rect = new Squid.Rectangle(0, 0, Texture.Resource.Description.Width, Texture.Resource.Description.Height);
//                RendererSlimDX renderer = Gui.Renderer as RendererSlimDX;
//                renderer.DrawTexture(Texture, Location.x, Location.y, Size.x, Size.y, rect, color);
//            }
//        }
//    }

//    public class ProgressBar : Control
//    {
//        public Label Progress { get; private set; }
//        public float Value { get; set; }

//        public ProgressBar()
//        {
//            Size = new Point(100,30);

//            Progress = new Label();
//            Progress.Dock = DockStyle.Left;
//            Progress.Size = new Point(50, 10);
//            Elements.Add(Progress);
//        }
//    }

//    public class NoiseFrame : GuiWindow
//    {
//        public Noise2D Module { get; private set; }
//        private Button BtnGenerate;
//        private Button BtnSave;
//        //private InPort Port;
//        private ImageFrame Image;
//        private PropertyFrame propertyGrid;

//        public NoiseFrame(Noise2D module)
//        {
//            Module = module;
//            InitializeComponent();
//        }

//        public NoiseFrame()
//        {
//            Module = new Noise2D(128);
//            InitializeComponent();
//        }

//        private void InitializeComponent()
//        {
//            MouseClick += (s, e) =>
//            {
//                Selector.SelectedObject = Module;
//                BringToFront();
//            };

//            Titlebar.MouseDown += (s, e) =>
//            {
//                Selector.SelectedObject = Module;
//                BringToFront();
//            };

//            Resizable = true;
//            Size = new Point(136, 160);

//            var obj = new GUIObject(Module);
//            //var pr = obj.GetProperty("Generator");
//            //Port = new InPort(pr, Module);
//            //Controls.Add(Port);

//            BtnGenerate = new Button();
//            BtnGenerate.MouseClick += BtnGenerate_MouseClick;
//            BtnGenerate.Size = new Point(100, 18);
//            BtnGenerate.Dock = DockStyle.Bottom;
//            BtnGenerate.Text = "Generate";
//            BtnGenerate.Style = "button";
//            Controls.Add(BtnGenerate);

//            BtnSave = new Button();
//            BtnSave.MouseClick += BtnSave_MouseClick;
//            BtnSave.Size = new Point(100, 18);
//            BtnSave.Dock = DockStyle.Bottom;
//            BtnSave.Text = "Save Image";
//            BtnSave.Style = "button";
//            Controls.Add(BtnSave);

//            propertyGrid = new PropertyFrame
//            {
//                Dock = DockStyle.Top
//            };
//            Controls.Add(propertyGrid);

//            int h = 50;
//            foreach (var info in obj.GetProperties())
//            {
//                if (info.Type.IsAssignableFrom(typeof(ModuleBase)))
//                {
//                    InPort conn = new InPort(info, Module);
//                    conn.Tag = 1;
//                    propertyGrid.Controls.Add(conn);
//                    h += 26;
//                }
//                else
//                {
//                    propertyGrid.AddProperty(info);
//                    h += 26;
//                }
//            }

//            propertyGrid.Size = new Point(136, h);
//            Size = new Point(136, 160 + h);

//            ProgressBar bar = new ProgressBar();
//            bar.Dock = DockStyle.Top;
//            bar.Style = "textbox";
//            bar.Progress.Style = "item";
//            bar.Progress.Text = "Progress:";
//            Controls.Add(bar);

//            Image = new ImageFrame();
//            Image.Size = new Point(128, 128);
//            Image.Dock = DockStyle.Fill;
//            Controls.Add(Image);

//            Titlebar.Text = "Image";

//            var btn = new Button();
//            btn.Size = new Point(24, 16);
//            btn.Style = "close";
//            btn.Tooltip = "Minimize";
//            btn.Dock = DockStyle.Right;
//            btn.Margin = new Margin(0, 2, 2, 2);
//            btn.Tint = ColorInt.ARGB(1, 0, 1, 1);
//            btn.MouseClick += (s, e) => { propertyGrid.ToggleProperties(false); };
//            this.Titlebar.GetElements().Add(btn);
//        }

//        void BtnSave_MouseClick(Control sender, MouseEventArgs args)
//        {
//            if (Image.Texture ==  null) return;

//            using (System.Windows.Forms.SaveFileDialog dlg = new System.Windows.Forms.SaveFileDialog())
//            {
//                //dlg.InitialDirectory = Editor.Directories.Content;
//                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
//                {
//                    // Resource.ToFile(Engine.Device.ImmediateContext, Image.Texture.Resource, ImageFileFormat.Dds, System.IO.Path.GetFileNameWithoutExtension(dlg.FileName) + ".dds");
//                    //Editor.Textures.SaveTexture(Image.Texture, dlg.FileName);
//                }
//            }
//        }

//        void BtnGenerate_MouseClick(Control sender, MouseEventArgs args)
//        {
//            if (Module.Generator != null)
//            {
//                Image.Texture = null;

//                Worker = new BackgroundWorker();
//                Worker.DoWork += new DoWorkEventHandler(Worker_DoWork);
//                Worker.RunWorkerAsync();
//            }
//        }

//        void Worker_DoWork(object sender, DoWorkEventArgs e)
//        {
//            Module.GeneratePlanar();

//            //Texture2D cube = Module.GetTextureCube(Proto.Gradient.Grayscale);

//            Texture2D texture = Module.GetTexture(Gradient.Grayscale);
//            ShaderResourceView view = new ShaderResourceView(Engine.Device, texture);

//            Image.Texture = new Texture { Resource = texture, View = view };
//        }

//        private BackgroundWorker Worker;
        
//    }

//    public class ModuleFrame : GuiWindow
//    {
//        public ModuleBase Module { get; private set; }
//        private PropertyFrame propertyGrid;

//        public ModuleFrame(ModuleBase module)
//        {
//            Module = module;

//            AllowDragOut = true;

//            InitializeComponent();
//        }

//        private void InitializeComponent()
//        {
//            MouseClick += (s, e) =>
//            {
//                Selector.SelectedObject = Module;
//                BringToFront();
//            };

//            Titlebar.MouseDown += (s, e) =>
//            {
//                Selector.SelectedObject = Module;
//                BringToFront();
//            };

//            Resizable = true;
//            propertyGrid = new PropertyFrame
//            {
//                Dock = DockStyle.Fill
//            };
//            Controls.Add(propertyGrid);

//            var obj = new GUIObject(Module);

//            int h = 50;
//            foreach (var info in obj.GetProperties())
//            {
//                if (info.Type.IsAssignableFrom(typeof(ModuleBase)))
//                {
//                    InPort conn = new InPort(info, Module);
//                    conn.Tag = new object();
//                    propertyGrid.Controls.Add(conn);
//                    h += 26;
//                }
//                else
//                {
//                    propertyGrid.AddProperty(info);
//                    h += 26;
//                }
//            }

//            propertyGrid.Controls.Add(new OutPort { Text = "Output", Instance = Module, Tag = new object() });
//            h += 20;
//            Size = new Point(140, h);
//            Titlebar.Text = Module.GetType().Name;
//            Titlebar.Style = "header";

//            Titlebar.Button.Visible = false;

//            //var btn = new Button();
//            //btn.Size = new Point(24, 16);
//            //btn.Style = "close";
//            //btn.Tooltip = "Minimize";
//            //btn.Dock = DockStyle.Right;
//            //btn.Margin = new Margin(0, 2, 2, 2);
//            //btn.Tint = ColorInt.ARGB(1, 0, 1, 1);
//            //btn.MouseClick += (s, e) => { propertyGrid.ToggleProperties(false); };
//            //this.Titlebar.GetElements().Add(btn);
//        }

//    }



//    public class InPort : Control
//    {
//        public Plug Plug;
//        private Label Label;
//        private GUIProperty Info;
//        private object Owner;

//        public string Text
//        {
//            get { return Label.Text; }
//            set { Label.Text = value; }
//        }

//        public InPort(GUIProperty info, object owner)
//        {
//            Dock = DockStyle.Top;
//            Owner = owner;
//            Info = info;
//            InitializeComponent();
//        }

//        private void InitializeComponent()
//        {
//            Size = new Point(100, 20);
//            Dock = DockStyle.Top;

//            Plug = new Plug { Style = "inport" };
//            Plug.Type = PlugType.In;
//            Plug.Size = new Point(20, 20);
//            Plug.Dock = DockStyle.Left;
//            Plug.Owner = Owner;
//            Plug.Property = Info;

//            Label = new Label();
//            Label.Size = new Point(20, 20);
//            Label.Dock = DockStyle.Fill;
//            Label.Text = Info != null ? Info.Name : "Input";
//            Label.TextAlign = Alignment.MiddleLeft;

//            Elements.Add(Plug);
//            Elements.Add(Label);
//        }
//    }


//    public class OutPort : Control
//    {
//        public Plug Plug;
//        private Label Label;

//        public object Instance
//        {
//            get { return Plug.Owner; }
//            set { Plug.Owner = value; }
//        }

//        public string Text
//        {
//            get { return Label.Text; }
//            set { Label.Text = value; }
//        }

//        public OutPort()
//        {
//            Size = new Point(100, 20);
//            Dock = DockStyle.Top;

//            Plug = new Plug { Style = "outport" };
//            Plug.Type = PlugType.Out;
//            Plug.Size = new Point(20, 20);
//            Plug.Dock = DockStyle.Right;

//            Label = new Label();
//            Label.Size = new Point(20, 20);
//            Label.Dock = DockStyle.Fill;
//            Label.Text = "name";
//            Label.TextAlign = Alignment.MiddleRight;

//            Elements.Add(Plug);
//            Elements.Add(Label);
//        }
//    }


//}
