using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Spark;
using System.Reflection;
using System.IO;
using System.Collections;

namespace Spark.Editor
{
    public partial class EditorForm : Form
    {
        private List<byte[]> Clipboard = new List<byte[]>();

        private ResourceBrowser Browser;
        private SceneExplorer Explorer;
        private Inspector Inspector;
        private OutputWindow OutputWindow;
        public Viewport MainViewport;
        public Viewport Viewport1;
        public Viewport Viewport2;
        public Viewport Viewport3;

        public EditorForm()
        {
            InitializeComponent();

            MainViewport = new Viewport();
            Viewport1 = new Viewport();
            Viewport2 = new Viewport();
            Viewport3 = new Viewport();
           
            OutputWindow = new OutputWindow();
            Explorer = new SceneExplorer();
            Inspector = new Inspector();
            Browser = new ResourceBrowser();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            MainViewport.CloseButton = false;
            MainViewport.Text = "Scene";
            MainViewport.Show(dockPanel1);

            Viewport1.Show(MainViewport.Pane, Spark.Windows.DockAlignment.Right, 0.25d);
            Viewport2.Show(Viewport1.Pane, Spark.Windows.DockAlignment.Bottom, 0.66d);
            Viewport3.Show(Viewport2.Pane, Spark.Windows.DockAlignment.Bottom, 0.5d);

            OutputWindow.Show(dockPanel1, Spark.Windows.DockState.DockBottom);
            Explorer.Show(dockPanel1, Spark.Windows.DockState.DockLeft);
            Inspector.Show(dockPanel1, Spark.Windows.DockState.DockRight);
            Browser.Show(Inspector.Pane, Spark.Windows.DockAlignment.Bottom, 0.4d);
            CreateMainMenu();
          
            //KeyMapper.Register(KeyBindAction.Undo, Undo);
            //KeyMapper.Register(KeyBindAction.Redo, Redo);
            //KeyMapper.Register(KeyBindAction.Cut, Cut);
            //KeyMapper.Register(KeyBindAction.Copy, Copy);
            //KeyMapper.Register(KeyBindAction.Paste, Paste);
            //KeyMapper.Register(KeyBindAction.ClearSelection, ClearSelection);
            //KeyMapper.Register(KeyBindAction.ShowSelection, ShowSelection);
            //KeyMapper.Register(KeyBindAction.HideSelection, HideSelection);
            //KeyMapper.Register(KeyBindAction.ToggleSelection, ToggleSelection);

            //MapItem(KeyBindAction.ClearSelection, clearToolStripMenuItem);
            //MapItem(KeyBindAction.FocusSelection, focusToolStripMenuItem);
            //MapItem(KeyBindAction.CenterSelection, centerToolStripMenuItem);
            //MapItem(KeyBindAction.ShowSelection, showToolStripMenuItem);
            //MapItem(KeyBindAction.HideSelection, hideToolStripMenuItem);
            //MapItem(KeyBindAction.ToggleSelection, toggleToolStripMenuItem);

            //MapItem(KeyBindAction.Undo, undoToolStripMenuItem);
            //MapItem(KeyBindAction.Redo, redoToolStripMenuItem);
            //MapItem(KeyBindAction.Cut, cutToolStripMenuItem);
            //MapItem(KeyBindAction.Copy, copyToolStripMenuItem);
            //MapItem(KeyBindAction.Paste, pasteToolStripMenuItem);

            // Engine.Initialize();

            MainViewport.Focus();

            Editor.LoadScene();

            var mainView = new RenderView(MainViewport.Handle);
            mainView.Pipeline = new DeferredRenderer();

            var view1 = new RenderView(Viewport1.Handle);
            view1.Pipeline = new DeferredRenderer();

            var view2 = new RenderView(Viewport2.Handle);
            view2.Pipeline = new DeferredRenderer();

            var view3 = new RenderView(Viewport3.Handle);
            view3.Pipeline = new DeferredRenderer();

            Editor.MainCamera.Target = mainView;
            Editor.Camera1.Target = view1;
            Editor.Camera2.Target = view2;
            Editor.Camera3.Target = view3;

            MainViewport.View = mainView;
            Viewport1.View = view1;
            Viewport2.View = view2;
            Viewport3.View = view3;

            MainViewport.Camera = Editor.MainCamera.Entity.GetComponent<EditorCamera>();
            Viewport1.Camera = Editor.Camera1.Entity.GetComponent<EditorCamera>();
            Viewport2.Camera = Editor.Camera2.Entity.GetComponent<EditorCamera>();
            Viewport3.Camera = Editor.Camera3.Entity.GetComponent<EditorCamera>();

            var scene = new EditorScene();

            Explorer.RefreshExplorer();

            dockPanel1.ActiveDocumentChanged += new EventHandler(dockPanel1_ActiveDocumentChanged);

            Engine.Run(this);
        }

        void dockPanel1_ActiveDocumentChanged(object sender, EventArgs e)
        {
            foreach (Viewport vp in dockPanel1.Documents)
            {
                vp.Camera.Entity.GetComponent<Camera>().Depth = 0;
                vp.Camera.Enabled = false;
            }

            Viewport vwp = dockPanel1.ActiveDocument as Viewport;
            if (vwp == null) return;

            vwp.Camera.Enabled = true;
            vwp.Camera.Entity.GetComponent<Camera>().Depth = -1;
            Input.SetHandle(vwp.Handle);
        }

        private void CreateMainMenu()
        {
            List<Type> types = Reflector.GetTypes<Spark.Component>();
            types.Sort((a, b) => a.Name.CompareTo(b.Name));

            List<string> categoryNames = new List<string>();

            foreach (Type type in types)
            {
                if (type.IsAbstract)
                    continue;

                object[] attributes = type.GetCustomAttributes(typeof(BrowsableAttribute), false);
                if (attributes.Length == 1)
                {
                    BrowsableAttribute att = attributes[0] as BrowsableAttribute;
                    if (!att.Browsable)
                        continue;
                }

                string assemblyName = type.Assembly.GetName().Name;

                if (!componentToolStripMenuItem.DropDownItems.ContainsKey(assemblyName))
                {
                    ToolStripMenuItem child = new ToolStripMenuItem();
                    child.Name = assemblyName;
                    child.Text = assemblyName;
                    componentToolStripMenuItem.DropDownItems.Add(child);
                }

                ToolStripItem parent = componentToolStripMenuItem.DropDownItems[assemblyName];

                object[] category = type.GetCustomAttributes(typeof(CategoryAttribute), true);
                if (category.Length == 0)
                {
                    ToolStripMenuItem item = new ToolStripMenuItem(type.Name);
                    item.Tag = type;
                    //item.Click += CreateComponent_Click;

                    (parent as ToolStripMenuItem).DropDownItems.Add(item);
                }
            }

            foreach (Type type in types)
            {
                if (type.IsAbstract)
                    continue;

                object[] attributes = type.GetCustomAttributes(typeof(BrowsableAttribute), false);
                if (attributes.Length == 1)
                {
                    BrowsableAttribute att = attributes[0] as BrowsableAttribute;
                    if (!att.Browsable)
                        continue;
                }

                string assemblyName = type.Assembly.GetName().Name;

                ToolStripMenuItem item = new ToolStripMenuItem(type.Name);
                item.Tag = type;
                //item.Click += CreateComponent_Click;

                ToolStripMenuItem parent = componentToolStripMenuItem.DropDownItems[assemblyName] as ToolStripMenuItem;
                string catname = string.Empty;

                object[] category = type.GetCustomAttributes(typeof(CategoryAttribute), true);
                if (category.Length > 0)
                {
                    CategoryAttribute cat = category[0] as CategoryAttribute;
                    catname = cat.Category;

                    if (parent.DropDownItems.ContainsKey(catname))
                    {
                        parent = parent.DropDownItems[catname] as ToolStripMenuItem;
                    }
                    else
                    {
                        ToolStripMenuItem child = new ToolStripMenuItem();
                        child.Name = catname;
                        child.Text = catname;
                        parent.DropDownItems.Add(child);
                        parent = child;
                    }

                    parent.DropDownItems.Add(item);
                }
            }

        
            //MethodInfo[] methods = typeof(Prototypes).GetMethods(BindingFlags.Static | BindingFlags.Public);
            //Array.Sort(methods, new Comparison<MethodInfo>((a, b) => a.Name.CompareTo(b.Name)));

            //foreach (MethodInfo method in methods)
            //{
            //    if (method.GetParameters().Length > 0) continue;

            //    ToolStripMenuItem item = new ToolStripMenuItem(method.Name.Replace("Create", ""));
            //    item.Tag = method;
            //    //item.Click += CreateEntity_Click;

            //    ToolStripItem parent = entityToolStripMenuItem;
            //    string catname = string.Empty;

            //    object[] category = method.GetCustomAttributes(typeof(CategoryAttribute), true);
            //    if (category.Length == 0)
            //        entityToolStripMenuItem.DropDownItems.Add(item);
            //}

            //entityToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());

            //foreach (MethodInfo method in methods)
            //{
            //    if (method.GetParameters().Length > 0) continue;

            //    ToolStripMenuItem item = new ToolStripMenuItem(method.Name.Replace("Create", ""));
            //    item.Tag = method;
            //    //item.Click += CreateEntity_Click;

            //    ToolStripItem parent = entityToolStripMenuItem;
            //    string catname = string.Empty;

            //    object[] category = method.GetCustomAttributes(typeof(CategoryAttribute), true);
            //    if (category.Length > 0)
            //    {
            //        CategoryAttribute cat = category[0] as CategoryAttribute;
            //        catname = cat.Category;

            //        if (entityToolStripMenuItem.DropDownItems.ContainsKey(catname))
            //        {
            //            parent = entityToolStripMenuItem.DropDownItems[catname];
            //        }
            //        else
            //        {
            //            ToolStripMenuItem child = new ToolStripMenuItem();
            //            child.Name = catname;
            //            child.Text = catname;
            //            entityToolStripMenuItem.DropDownItems.Add(child);
            //        }

            //        parent = entityToolStripMenuItem.DropDownItems[catname];
            //        (parent as ToolStripMenuItem).DropDownItems.Add(item);
                
            //    }
            //}

            //foreach (IPlugin plugin in PluginServices.Plugins)
            //{
            //    ToolStripMenuItem item = new ToolStripMenuItem(plugin.Name);
            //    item.Tag = plugin;
            //    item.Click += new EventHandler(plugin_Click);
            //    pluginsToolStripMenuItem.DropDownItems.Add(item);
            //}
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.ShowDialog();
        }
    }
}
