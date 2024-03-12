using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Squid;

namespace Spark.Editor
{
    public class AssetBrowser : GuiWindow
    {
        private TreeView Treeview;
        private SplitContainer split;
        private ScrollPanel scrollPanel;
        private Frame toolbar;
        private Frame bottombar;
        private FlowLayoutFrame flow;
        private SearchBox searchbox;

        private IAsset selectedAsset;

        private static AssetBrowser staticInstance;

        public static void Open(Desktop desktop, Type assetType)
        {
            if(staticInstance == null)
                staticInstance = new AssetBrowser();

            staticInstance.Show(desktop);
            staticInstance.PopulateFromAssetType(assetType); 
        }

        public static void SelectAsset(IAsset asset)
        {
            staticInstance.Select(asset);
        }

        public AssetBrowser()
        {
            Style = "window";
            Size = new Point(800, 600);
            Modal = true;
            Resizable = true;
            Dock = DockStyle.Center;

            toolbar = new Frame { Style = "frame", Size = new Point(16, 24), Dock = DockStyle.Top };
            toolbar.Margin = new Margin(0, 0, 0, 1);

            //split = new SplitContainer();
            //split.Dock = DockStyle.Fill;
            //split.RetainAspect = false;
            //split.Orientation = Orientation.Horizontal;
            //split.SplitFrame1.Size = new Point(200, 200);
            //split.SplitButton.Size = new Point(2, 2);
            //split.SplitButton.Style = "frame";
            //split.SplitButton.Margin = new Margin(1, 0, 1, 0);

            //Treeview = new TreeView();
            //Treeview.Style = "frame";
            //Treeview.Dock = DockStyle.Fill;
            //Treeview.Scrollbar.Size = new Point(12, 16);
            //Treeview.Scrollbar.ButtonDown.Visible = false;
            //Treeview.Scrollbar.ButtonUp.Visible = false;
            //Treeview.Scrollbar.Slider.Button.Margin = new Margin(2, 4, 0, 4);
            //Treeview.Scrollbar.Slider.Ease = false;
            //Treeview.Scrollbar.Slider.MinHandleSize = 64;
            //Treeview.Indent = 8;
            //Treeview.SelectedNodeChanged += Treeview_SelectedNodeChanged;

            bottombar = new Frame
            {
                Style = "frame",
                Size = new Point(16, 24),
                Dock = DockStyle.Bottom,
                Margin = new Margin(0, 1, 0, 0)
            };

            searchbox = new SearchBox
            {
                Size = new Point(200, 16),
                Dock = DockStyle.Right,
                Margin = new Margin(2)
            };

            searchbox.TextChanged += Searchbox_TextChanged;

            scrollPanel = new ScrollPanel
            {
                Dock = DockStyle.Fill
            };

            flow = new FlowLayoutFrame
            {
                HSpacing = 8,
                VSpacing = 8,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = AutoSize.Vertical,
                Dock = DockStyle.Top
            };

            Controls.Add(toolbar);
            // Controls.Add(split);
            Controls.Add(bottombar);
            Controls.Add(scrollPanel);

            toolbar.Controls.Add(searchbox);
            //split.SplitFrame1.Controls.Add(Treeview);
            //split.SplitFrame2.Controls.Add(bottombar);
            //split.SplitFrame2.Controls.Add(scrollPanel);
            scrollPanel.Content.Controls.Add(flow);

            //var dir = new DirectoryInfo(Engine.Content.BaseDirectory);
            //var node = CreateNode(dir);
            //Treeview.Nodes.Add(node);
            //AddFolder(dir, node);
        }

        public void Select(IAsset asset)
        {

        }

        private void Searchbox_TextChanged(Control sender)
        {
            foreach(var item in flow.Controls)
                item.Visible = item.Name.Contains(searchbox.Text);

            flow.ForceFlowLayout();
        }

        private void Treeview_SelectedNodeChanged(Control sender, TreeNode value)
        {
            flow.Controls.Clear();

            if (value == null) return;

            var dir = value.Tag as DirectoryInfo;
            PopulateFromFolder(dir);
        }

        void PopulateFromFolder(DirectoryInfo dir)
        {
            var files = System.IO.Directory.GetFiles(dir.FullName);
            foreach (var file in files)
            {
                var filename = System.IO.Path.GetFileName(file);
                var assetType = AssetManager.GetAssetType(filename);

                if (assetType != null)
                {
                    var info = new AssetInfo(file, assetType);
                    AddItem(info);
                }
            }
        }

        void PopulateFromAssetType(Type assetType)
        {
            flow.Controls.Clear();

            var dir = new DirectoryInfo(Engine.ResourceDirectory);
            var extensions = AssetManager.GetExtensions(assetType).ToArray();
            var files = dir.GetFilesFiltered(extensions);
            
            foreach (var file in files)
            {
                var info = new AssetInfo(file.FullName, assetType);
                AddItem(info);
            }
        }

        void AddRoot(string path)
        {
            var info = new System.IO.DirectoryInfo(path);
            var node = CreateNode(info);
            AddFolder(info, node);
        }

        void AddFolder(DirectoryInfo dir, TreeNode parent)
        {
            var folders = System.IO.Directory.GetDirectories(dir.FullName);

            foreach (var childdir in folders)
            {
                var info = new System.IO.DirectoryInfo(childdir);
                var node = CreateNode(info);

                if (parent != null)
                    parent.Nodes.Add(node);
                else
                    Treeview.Nodes.Add(node);

                AddFolder(info, node);
            }
        }
            
        TreeNode CreateNode(DirectoryInfo dir)
        {
            TreeNodeEx node = new TreeNodeEx();
            node.Tag = dir;
            node.Value = dir;
            node.Style = "";
            node.Margin = new Margin(0, 0, 0, 0);
            node.Size = new Point(26, 18);
            node.Label.Text = dir.Name;
            node.Label.Style = "node";
            node.Button.Style = "node";
            node.Button.Margin = new Margin(0, 0, 0, 0);
            return node;
        }

        void AddItem(AssetInfo asset)
        {
            var size = 96;
            var extra = (int)(size / 1.8f);

            var relativePath = asset.FullPath.Replace(AssetDatabase.Assets.FullName, string.Empty);
            var guid = AssetDatabase.PathToGuid(relativePath);

            var item = new Button { };
            item.Size = new Point(size, size + extra);
            item.Style = "tile";
            //item.MouseClick += (s, e) => { Selector.SelectedObject = AssetDatabase.GetAssetImporter(asset.FullPath); };
            item.Tooltip = asset.Name;

            var dropshadow = new Frame();
            dropshadow.Dock = DockStyle.Fill;
            dropshadow.Style = "dropshadow";
            dropshadow.NoEvents = true;
            item.GetElements().Add(dropshadow);

            var image = new ImageControl
            {
                Size = new Point(size, size),
                Dock = DockStyle.Top,
                Margin = new Margin(0, 0, 1, 0),
                NoEvents = true,
                Style = "dark2",
                Texture = $"@{guid}.png"
            };
            item.GetElements().Add(image);
            item.Name = asset.Name;

            var lblName = new Label
            {
                Text = asset.Name,
                Size = new Point(100, 16),
                Dock = DockStyle.Top,
                Margin = new Margin(2, 2, 2, 0),
                NoEvents = true
            };

            var lblType = new Label
            {
                Text = asset.AssetType.Name.ToUpperInvariant(),
                TextAlign = Alignment.MiddleRight,
                Size = new Point(100, 16),
                Dock = DockStyle.Bottom,
                UseTextColor = true,
                Margin = new Margin(2, 0, 4, 2),
                TextColor = ColorInt.ARGB(1, .5f, .5f, .5f),
                NoEvents = true
            };

            var lblSize = new Label
            {
                Text = asset.FileSize,
                Size = new Point(100, 16),
                Dock = DockStyle.Bottom,
                UseTextColor = true,
                TextColor = ColorInt.ARGB(1, .5f, .5f, .5f),
                NoEvents = true
            };

            item.GetElements().Add(lblName);
            item.GetElements().Add(lblType);

            flow.Controls.Add(item);
        }
    }

}
