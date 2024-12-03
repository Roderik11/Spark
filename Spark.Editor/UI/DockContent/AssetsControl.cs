using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squid;
using Spark;
using System.Reflection;
using System.IO;
using static Spark.Editor.MenuBuilder;
using SharpDX.Direct3D11;

namespace Spark.Editor
{
    public class AssetsControl : Frame
    {
        private VirtualList VirtualList;
        private SplitContainer split;
        private ScrollPanel scrollPanel;
        private Frame toolbarLeft;
        private Frame toolbarRight;

        private Frame bottombar;
        private FlowLayoutFrame flow;
        private SearchBox searchbox;
        private Frame breadcrumb;

        private List<Directory> directories = new List<Directory>();

        public class ExplorerNode : Button
        {
            public Control IndentFrame { get; private set; }
            public ImageControl Foldout { get; private set; }
            public ImageControl Icon { get; private set; }
            public Label Label { get; private set; }

            public int Indent = 12;

            public event Action<int, Directory> ExpandedChanged;

            public int NodeIndex { get; private set; }
            public Directory Directory { get; private set; }

            public static Directory SelectedNode;

            public ExplorerNode()
            {
                Size = new Point(100, 18);
                Dock = DockStyle.Top;
                Style = "";

                IndentFrame = new Control()
                {
                    Style = "node",
                    NoEvents = true,
                    Dock = DockStyle.Left,
                };

                Foldout = new ImageControl
                {
                    Style = "node",
                    NoEvents = true,
                    Size = new Point(20, 20),
                    Dock = DockStyle.Left,
                    Enabled = false,
                    Tiling = TextureMode.Center,
                    Color = ColorInt.ARGB(1f, .5f, .5f, .5f)
                };

                Icon = new ImageControl
                {
                    Style = "node",
                    NoEvents = true,
                    Size = new Point(16, 20),
                    Dock = DockStyle.Left,
                    Tiling = TextureMode.Center,
                    Texture = "folder.dds",
                    Color = ColorInt.ARGB(1f, .5f, .5f, .5f)
                };

                Label = new Button
                {
                    Style = "node",
                    NoEvents = true,
                    Size = new Point(20, 20),
                    Dock = DockStyle.Fill,
                };

                Elements.Add(IndentFrame);
                Elements.Add(Foldout);
                Elements.Add(Icon);
                Elements.Add(Label);

                Foldout.MouseClick += Foldout_MouseClick;
            }

            public void Bind(Directory directory, int index)
            {
                NodeIndex = index;
                Directory = directory;
                Tag = directory;

                int childCOunt = directory.ChildCount;

                Foldout.Enabled = childCOunt > 0;
                Foldout.NoEvents = childCOunt == 0;
                Foldout.Texture = childCOunt > 0 ? "nav_right.dds" : "";
                Foldout.Texture = directory.Expanded ? "nav_down.dds" : Foldout.Texture;
                Selected = SelectedNode?.Info.FullName == directory.Info.FullName;
                if (Selected)Focus();

                Label.Text = directory.Info.Name;
                IndentFrame.Size = new Point(Indent * directory.Depth, IndentFrame.Size.y);
            }

            protected override void OnStateChanged()
            {
                IndentFrame.State = State;
                Foldout.State = State;
                Label.State = State;
                Icon.State = State;
                Foldout.State = State;
            }

            void Foldout_MouseClick(Control sender, MouseEventArgs args)
            {
                if (args.Button > 0) return;
                ExpandedChanged?.Invoke(NodeIndex, Directory);
            }
        }

        public class Directory
        {
            static HashSet<string> expandedState = new HashSet<string>();

            public Directory Parent;
            public DirectoryInfo Info;
            public int ChildCount;

            public Directory(Directory parent, DirectoryInfo info, int depth)
            {
                Parent = parent;
                Info = info;
                Depth = depth;
                ChildCount = info.GetDirectories().Length;
            }

            public bool Expanded
            {
                get => expandedState.Contains(Info.FullName);
                set
                {
                    if (value)
                    {
                        if (!expandedState.Contains(Info.FullName));
                            expandedState.Add(Info.FullName);
                    }
                    else
                        expandedState.Remove(Info.FullName);
                }
            }

            public int Depth;
        }

        public AssetsControl()
        {
            Style = "window";

            toolbarLeft = new Frame { Style = "header", Size = new Point(16, 28), Dock = DockStyle.Top };
            toolbarLeft.Margin = new Margin(0, 0, 0, 1);

            toolbarRight = new Frame { Style = "header", Size = new Point(16, 28), Dock = DockStyle.Top };
            toolbarRight.Margin = new Margin(0, 0, 0, 1);

            split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.RetainAspect = false;
            split.Orientation = Orientation.Horizontal;
            split.SplitFrame1.Size = new Point(200, 200);
            split.SplitButton.Size = new Point(2, 2);
            split.SplitButton.Style = "frame";
            split.SplitButton.Margin = new Margin(1, 0, 1, 0);

            VirtualList = new VirtualList();
            VirtualList.Dock = DockStyle.Fill;
            VirtualList.Scrollbar.ButtonDown.Visible = false;
            VirtualList.Scrollbar.ButtonUp.Visible = false;
            VirtualList.Scrollbar.Slider.Ease = false;
            VirtualList.Scrollbar.Slider.MinHandleSize = 64;
            VirtualList.Content.Style = "";
            VirtualList.ItemHeight = 18;
            VirtualList.CreateItem = CreateNode;
            VirtualList.BindItem = BindNode;

            bottombar = new Frame
            {
                Style = "frame",
                Size = new Point(16, 20),
                Dock = DockStyle.Bottom,
                Margin = new Margin(0, 1, 0, 0)
            };

            searchbox = new SearchBox
            {
                Size = new Point(200, 16),
                Dock = DockStyle.Left,
                Margin = new Margin(2)
            };

            searchbox.TextChanged += Searchbox_TextChanged;

            scrollPanel = new ScrollPanel
            {
                Dock = DockStyle.Fill,
                Style = "frame",
            };

            scrollPanel.VScroll.Ease = true;

            flow = new FlowLayoutFrame
            {
                HSpacing = 8,
                VSpacing = 8,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = AutoSize.Vertical,
                Dock = DockStyle.Top,
            };

            breadcrumb = new Frame
            {
                Dock = DockStyle.Left,
                AutoSize= AutoSize.Horizontal,
                Margin = new Margin(24,0,0,0)
            };

            // Controls.Add(toolbar);
            Controls.Add(split);

            toolbarRight.Controls.Add(searchbox);
            toolbarRight.Controls.Add(breadcrumb);
            split.SplitFrame1.Controls.Add(toolbarLeft);
            split.SplitFrame1.Controls.Add(VirtualList);

            split.SplitFrame2.Controls.Add(toolbarRight);
            split.SplitFrame2.Controls.Add(bottombar);
            split.SplitFrame2.Controls.Add(scrollPanel);
            scrollPanel.Content.Controls.Add(flow);

            var baseDir = Engine.Assets.BaseDirectory;
            baseDir = baseDir.Substring(0, baseDir.Length - 1);
            var root = new DirectoryInfo(baseDir);
            var dirs = root.GetDirectories();

            var resourceDirectory = new Directory(null, root, 0);
            resourceDirectory.Expanded = true;
            directories.Add(resourceDirectory);

            foreach (var dir in dirs)
                directories.Add(new Directory(resourceDirectory, dir, 1));
            VirtualList.DataSource = directories;

            var btn = CreateMenuItem(toolbarLeft, "Create Asset", false);
            var types = Reflector.GetTypes<Asset>();
            foreach (var type in types)
            {
                if(!type.IsAbstract)
                    AddMenuItem(btn, type.Name, (s, a) => { });
            }
        }

        void FindExpandedChildren(Directory parent, List<Directory> result)
        {
            var dirs = parent.Info.GetDirectories();
            foreach(var dir in dirs)
            {
                var sub = new Directory(parent, dir, parent.Depth + 1);
                result.Add(sub);

                if(sub.Expanded)
                    FindExpandedChildren(sub, result);
            }
        }

        private void ExpandTo(DirectoryInfo directory)
        {
            var target = directories.Find((e) => e.Info.FullName == directory.FullName);

            if (target == null)
            {
                var root = directories[0];

                if (root.Expanded)
                    CollapseNode(0, root);

                DirectoryInfo parentDir = directory;
                while (parentDir.Parent != null)
                {
                    parentDir = parentDir.Parent;
                    var temp = new Directory(null, parentDir, 0);
                    temp.Expanded = true;
                    if (AssetDatabase.Assets.FullName.Contains(parentDir.FullName))
                        break;
                }

                if (root.Expanded)
                    ExpandNode(0, root);

                target = directories.Find((e) => e.Info.FullName == directory.FullName);
            }

            ExplorerNode.SelectedNode = target;
            VirtualList.Refresh();
            VirtualList.UpdateVirtualList();
            VirtualList.ScrollTo(directories.IndexOf(target));
        }

        private void BindNode(Control control, int index)
        {
            var node = control as ExplorerNode;
            var entity = directories[index];
            node.Bind(entity, index);
        }

        private ExplorerNode CreateNode(int index)
        {
            var directory = directories[index];

            ExplorerNode node = new ExplorerNode();
            node.Indent = 12;
            node.Tag = directory;
            node.MouseClick += Node_MouseClick;
            node.ExpandedChanged += Node_ExpandedChanged;
            node.AllowFocus = true;
            node.Bind(directory, index);

            node.KeyDown += (s, e) =>
            {
                var dir = s.Tag as Directory;
                var myIndex = directories.IndexOf(dir);

                if (e.Key == Keys.RIGHTARROW && !dir.Expanded)
                    Node_ExpandedChanged(myIndex, dir);

                if (e.Key == Keys.LEFTARROW && dir.Expanded)
                    Node_ExpandedChanged(myIndex, dir);
                
                if (e.Key == Keys.DOWNARROW)
                {
                    var nextIndex = Math.Min(myIndex + 1, directories.Count - 1);
                    var nextDir = directories[nextIndex];
                    SelectNode(nextDir);
                    VirtualList.ScrollTo(nextIndex);
                }

                if (e.Key == Keys.UPARROW)
                {
                    var prev = Math.Max(myIndex - 1, 0);
                    var nextDir = directories[prev];
                    SelectNode(nextDir);
                    VirtualList.ScrollTo(prev);
                };
            };

            return node;
        }

        private void Node_MouseClick(Control sender, MouseEventArgs args)
        {
            SelectNode(sender.Tag as Directory);
        }


        void SelectNode(Directory dir)
        {
            ExplorerNode.SelectedNode = dir;
            VirtualList.Refresh();

            flow.Controls.Clear();
            PopulateFromFolder(dir.Info);
            BuildBreadCrumb(dir.Info);
        }

        private void Node_ExpandedChanged(int index, Directory directory)
        {
            directory.Expanded = !directory.Expanded;

            if (directory.Expanded)
                ExpandNode(index, directory);
            else
                CollapseNode(index, directory);

            VirtualList.Refresh();
            VirtualList.UpdateVirtualList();
        }

        private void CollapseNode(int index, Directory directory)
        {
            var list = new List<Directory>();
            FindExpandedChildren(directory, list);
            directories.RemoveRange(index + 1, list.Count);
        }

        private void ExpandNode(int index, Directory directory)
        {
            var list = new List<Directory>();
            FindExpandedChildren(directory, list);
            directories.InsertRange(index + 1, list);
        }

        private void BuildBreadCrumb(DirectoryInfo directory)
        {
            List<Control> controls = new List<Control>();

            var button = new Button
            {
                Text = directory.Name,
                AutoSize = AutoSize.Horizontal,
                Dock = DockStyle.Left,
                Tag = directory
            };
            button.MouseClick += (s, e) =>
            {
                ExpandTo(s.Tag as DirectoryInfo);
                flow.Controls.Clear();
                PopulateFromFolder(s.Tag as DirectoryInfo);
                BuildBreadCrumb(s.Tag as DirectoryInfo);
            };
            controls.Add(button);

            if (!AssetDatabase.Assets.FullName.Contains(directory.FullName))
            {
                DirectoryInfo parentDir = directory;

                while (parentDir.Parent != null)
                {
                    parentDir = parentDir.Parent;

                    var btn = new Button
                    {
                        Text = parentDir.Name,
                        AutoSize = AutoSize.Horizontal,
                        Dock = DockStyle.Left,
                        Tag = parentDir
                    };
                    btn.MouseClick += (s, e) =>
                    {
                        ExpandTo(s.Tag as DirectoryInfo);
                        flow.Controls.Clear();
                        PopulateFromFolder(s.Tag as DirectoryInfo);
                        BuildBreadCrumb(s.Tag as DirectoryInfo);
                    };

                    controls.Add(btn);

                    if (AssetDatabase.Assets.FullName.Contains(parentDir.FullName))
                        break;
                }
            }

            controls.Reverse();

            breadcrumb.Controls.Clear();

            foreach(var control in controls)
            {
                breadcrumb.Controls.Add(control);

                var img = new ImageControl
                {
                    NoEvents = true,
                    Texture = "icon_right.png",
                    Size = new Point(28, 28),
                    Dock = DockStyle.Left,
                    Tiling = TextureMode.Center
                };
                breadcrumb.Controls.Add(img);
            }
        }

        private void Searchbox_TextChanged(Control sender)
        {
            bool isempty = string.IsNullOrEmpty(searchbox.Text);

            flow.Controls.Clear();

            if (isempty)
            {
                var dir = ExplorerNode.SelectedNode;
                if (dir != null)
                {
                    PopulateFromFolder(dir.Info);
                    BuildBreadCrumb(dir.Info);
                }

                return;
            }

            var files = System.IO.Directory.GetFiles(Engine.Assets.BaseDirectory, $"*{searchbox.Text}*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var filename = System.IO.Path.GetFileName(file);
                var assetType = AssetManager.GetAssetType(filename);

                if (assetType != null)
                {
                    var info = new AssetInfo(file, assetType);
                    AddAssetCard(info);
                }
            }
        }

        void PopulateFromFolder(DirectoryInfo dir)
        {
            var directories = System.IO.Directory.GetDirectories(dir.FullName);
            foreach (var sub in directories)
            {
                var info = new DirectoryInfo(sub);
                AddFolderCard(info);
            }

            var files = System.IO.Directory.GetFiles(dir.FullName);
            foreach (var file in files)
            {
                var filename = System.IO.Path.GetFileName(file);
                var assetType = AssetManager.GetAssetType(filename);

                if (assetType != null)
                {
                    var info = new AssetInfo(file, assetType);
                    AddAssetCard(info);
                }
            }
        }

        void AddFolderCard(DirectoryInfo dir)
        {
            var size = 96;
            var extra = (int)(size / 1.8f);

            var item = new Button { };
            item.Size = new Point(size, size + extra);
            item.Style = "tile";
            item.Tag = dir;
            item.MouseClick += (s, e) =>
            {
                ExpandTo(s.Tag as DirectoryInfo);
            };

            item.MouseDoubleClick += (s, e) =>
            {
                //ExpandTo(d);
                flow.Controls.Clear();
                PopulateFromFolder(s.Tag as DirectoryInfo);
                BuildBreadCrumb(s.Tag as DirectoryInfo);
            };
            item.Tooltip = dir.FullName;

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
                Texture = $"icon_folder.png"
            };
            item.GetElements().Add(image);

            var lblName = new Label
            {
                Text = dir.Name,
                Size = new Point(100, 16),
                Dock = DockStyle.Top,
                Margin = new Margin(2, 2, 2, 0),
                NoEvents = true
            };

            var lblType = new Label
            {
                Text = "DIRECTORY",
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
                Text = "",
                Size = new Point(100, 16),
                Dock = DockStyle.Bottom,
                UseTextColor = true,
                TextColor = ColorInt.ARGB(1, .5f, .5f, .5f),
                NoEvents = true
            };

            item.GetElements().Add(lblName);
            item.GetElements().Add(lblType);
            //item.GetElements().Add(lblSize);

            flow.Controls.Add(item);

            //item.MouseDrag += (s, e) =>
            //{
            //    var drag = new ImageControl
            //    {
            //        Tag = asset,
            //        Size = new Point(size, size),
            //        NoEvents = true,
            //        Style = "dark2",
            //        Texture = $"@{guid}.png"
            //    };
            //    drag.Position = Gui.MousePosition - drag.Size / 2;
            //    DoDragDrop(drag);
            //};
        }

        void AddAssetCard(AssetInfo asset)
        {
            var size = 96;
            var extra = (int)(size / 1.8f);

            var relativePath = asset.FullPath.Replace(AssetDatabase.Assets.FullName, string.Empty);
            var guid = AssetDatabase.PathToGuid(relativePath);

            var item = new Button { };
            item.Size = new Point(size, size + extra);
            item.Style = "tile";
            item.MouseClick += (s, e) =>
            {
                Selector.SelectedObject = AssetDatabase.GetAssetReader(asset.FullPath);
            };
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
            //item.GetElements().Add(lblSize);

            flow.Controls.Add(item);

            item.MouseDrag += (s, e) =>
            {
                var drag = new ImageControl
                {
                    Tag = asset,
                    Size = new Point(size, size),
                    NoEvents = true,
                    Style = "dark2",
                    Texture = $"@{guid}.png"
                };
                drag.Position = Gui.MousePosition - drag.Size / 2;
                DoDragDrop(drag);
            };
        }
    }

}
