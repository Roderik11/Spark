using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squid;
using static Spark.Editor.MenuBuilder;

namespace Spark.Editor
{
    public class EditorDesktop : Desktop
    {
        public DockGroup Explorer;
        public DockGroup Inspector;
        public DockGroup Assets;
        public DockGroup Settings;
        public DockGroup Scene;
        public DockGroup Debug;

        public DockRegion DockArea;

        public Frame statusBar;

        private List<string> lastCommands = new List<string>();
        private string currentstring;
        private int cmdIndex;

        void CreateSkin()
        {
            //Gui.SetSkin(Gui.GenerateStandardSkin());
            //MapStyles("SampleMap");

            float b = .0061f;

            int grey10 = ColorInt.ARGB(1f, .10f - b, .10f - b, .10f + b);
            int grey125 = ColorInt.ARGB(1f, .125f - b, .125f - b, .125f + b);
            int grey15 = ColorInt.ARGB(1f, .15f - b, .15f - b, .15f + b);
            int grey166 = ColorInt.ARGB(1f, .166f - b, .166f - b, .166f + b);
            int grey170 = ColorInt.ARGB(1f, .170f - b, .170f - b, .170f + b);
            int grey175 = ColorInt.ARGB(1f, .175f - b, .175f - b, .175f + b);
            int grey20 = ColorInt.ARGB(1f, .20f - b, .20f - b, .20f + b);
            int grey25 = ColorInt.ARGB(1f, .25f - b, .25f - b, .25f + b);
            int grey30 = ColorInt.ARGB(1f, .30f - b, .30f - b, .30f + b);
            int grey35 = ColorInt.ARGB(1f, .35f - b, .35f - b, .35f + b);
            int grey40 = ColorInt.ARGB(1f, .40f - b, .40f - b, .40f + b);
            int textColor = ColorInt.ARGB(1f, .80f, .80f, .80f);
            int darkColor = grey10;// ColorInt.ARGB(.95f, .1f, .1f, .1f);

            int windowColor = darkColor;
            int normalColor = grey125;
            int hotColor = grey20;
            int selectedColor = grey25;

            ControlStyle baseStyle = new ControlStyle();
            baseStyle.Font = "roboto_regular_10";
            //baseStyle.Font = "roboto_medium_10";
            //baseStyle.Font = "roboto_medium_9";
            //baseStyle.Font = "roboto_regular_9";
            //baseStyle.Font = "roboto_bold_9";
            baseStyle.TextColor = textColor;

            ControlStyle colorstyle = new ControlStyle(baseStyle);
            colorstyle.BackColor = -1;
            colorstyle.Texture = "border_black.dds";
            colorstyle.Tiling = TextureMode.Grid;
            colorstyle.Grid = new Squid.Margin(4);
            //colorstyle.Tint = ColorInt.ARGB(1, 0, 1, 0);

            ControlStyle dropshadow = new ControlStyle(baseStyle);
            dropshadow.Texture = "dropshadow.dds";
            dropshadow.Tiling = TextureMode.Grid;
            dropshadow.Grid = new Squid.Margin(4);
            dropshadow.Tint = ColorInt.ARGB(.7f, 1, 1, 1);
            ControlStyle window = new ControlStyle(baseStyle);
            window.BackColor = windowColor;

            ControlStyle frame = new ControlStyle(baseStyle);
            //frame.Texture = "rounded.dds";
            //frame.Grid = new Margin(3);
            //frame.Tiling = TextureMode.Grid;
            //frame.Tint = normalColor;
            frame.BackColor = normalColor;
            frame.TextPadding = new Squid.Margin(8, 0, 0, 0);

            ControlStyle category = new ControlStyle(baseStyle);
            category.BackColor = grey20;
            category.Hot.BackColor = grey25;
            category.TextPadding = new Squid.Margin(0, 0, 0, 0);
            category.TextAlign = Alignment.MiddleLeft;

            ControlStyle colorGrey170 = new ControlStyle();
            colorGrey170.BackColor = grey166;

            ControlStyle label = new ControlStyle(baseStyle);
            label.TextPadding = new Squid.Margin(2, 0, 0, 0);

            ControlStyle propertyLabel = new ControlStyle(category);
            propertyLabel.TextPadding = new Squid.Margin(28, 0, 0, 0);
            propertyLabel.BackColor = grey170;
            propertyLabel.Hot.BackColor = grey20;

            ControlStyle propertyElement = new ControlStyle(category);
            propertyElement.TextPadding = new Squid.Margin(0, 0, 0, 0);
            propertyElement.BackColor = grey166;
            propertyElement.Hot.BackColor = grey20;
            propertyElement.Selected.BackColor = grey25;
            propertyElement.SelectedHot.BackColor = grey25;
            propertyElement.SelectedPressed.BackColor = grey25;
            propertyElement.SelectedFocused.BackColor = grey25;
            propertyElement.Checked.BackColor = grey25;
            propertyElement.CheckedHot.BackColor = grey25;

            var header = new ControlStyle(propertyElement);
            header.TextPadding = new Squid.Margin(8, 0, 0, 0);
            header.TextAlign = Alignment.MiddleLeft;

            ControlStyle propertyIndent = new ControlStyle(category);
            propertyIndent.TextPadding = new Squid.Margin(0, 0, 0, 0);
            propertyIndent.BackColor = grey170;
            propertyIndent.Hot.BackColor = grey20;
            propertyIndent.Texture = "shadow_right.png";
            propertyIndent.Tiling = TextureMode.RepeatX;

            ControlStyle statusBarLabel = new ControlStyle(label);
            statusBarLabel.BackColor = grey15;
            statusBarLabel.TextPadding = new Margin(16, 0, 0, 0);

            var tab = new ControlStyle(baseStyle);
            tab.TextAlign = Alignment.MiddleLeft;
            tab.TextPadding = new Squid.Margin(8, 0, 0, 0);
            tab.BackColor = grey10;
            tab.Hot.BackColor = grey125;
            tab.Selected.BackColor = grey15;
            tab.SelectedHot.BackColor = grey20;
            tab.SelectedPressed.BackColor = grey20;
            tab.SelectedFocused.BackColor = grey20;
            tab.Checked.BackColor = grey15;
            tab.CheckedHot.BackColor = grey20;
            tab.Pressed.BackColor = grey20;
            tab.CheckedPressed.BackColor = grey20;
            tab.SelectedPressed.BackColor = grey20;

            ControlStyle button = new ControlStyle(baseStyle);
            button.TextAlign = Alignment.MiddleCenter;
            button.BackColor = normalColor;
            button.Hot.BackColor = hotColor;
            button.Selected.BackColor = selectedColor;
            button.SelectedHot.BackColor = selectedColor;
            button.SelectedPressed.BackColor = selectedColor;
            button.SelectedFocused.BackColor = selectedColor;
            button.Checked.BackColor = selectedColor;
            button.CheckedHot.BackColor = selectedColor;

            ControlStyle item = new ControlStyle(button);
            item.TextAlign = Alignment.MiddleLeft;
            item.TextPadding = new Squid.Margin(8, 0, 0, 0);
            item.BackColor = 0;
            item.Hot.BackColor = hotColor;
            item.Selected.BackColor = selectedColor;
            item.SelectedHot.BackColor = selectedColor;
            item.SelectedPressed.BackColor = selectedColor;
            item.SelectedFocused.BackColor = selectedColor;
            item.Checked.BackColor = selectedColor;
            item.CheckedHot.BackColor = selectedColor;

            var node = new ControlStyle(item);
            node.Default.BackColor = ColorInt.ARGB(0, 0, 0, 0);

            ControlStyle indent18 = new ControlStyle(item);
            indent18.Texture = "shadow_18.png";
            indent18.Tiling = TextureMode.Repeat;

            var menu = new ControlStyle(item);
            menu.Default.BackColor = ColorInt.ARGB(0, 0, 0, 0);
            menu.TextPadding = new Margin(8, 0, 8, 0);
            menu.TextAlign = Alignment.MiddleCenter;

            var menuitem = new ControlStyle(item);
            menuitem.Default.BackColor = ColorInt.ARGB(0, 0, 0, 0);
            menuitem.TextPadding = new Margin(32, 0, 8, 0);

            ControlStyle textbox = new ControlStyle(baseStyle);
            textbox.Texture = "shadow.png";
            textbox.TextAlign = Alignment.MiddleLeft;
            textbox.BackColor = grey10;
            //textbox.Hot.BackColor = ColorInt.RGBA(.25f, .25f, .25f, .5f);
            textbox.TextPadding = new Squid.Margin(6, 0, 6, 0);
            textbox.Hot.Texture = "border.dds";
            textbox.Focused.Texture = "border.dds";
            textbox.Tiling = TextureMode.Grid;
            textbox.Grid = new Squid.Margin(4);

            var dropdownLabel = new ControlStyle(baseStyle);
            dropdownLabel.TextPadding = new Squid.Margin(6, 0, 6, 0);
            
            var searchbox = new ControlStyle(textbox);
            searchbox.TextPadding = new Margin(20, 0, 20, 0);

            ControlStyle down = new ControlStyle();
            down.Texture = "icon_down.png";


            ControlStyle switchbutton = new ControlStyle(baseStyle);
            switchbutton.Texture = "switch_off.png";
            switchbutton.Checked.Texture = "switch_on.png";
            switchbutton.CheckedHot.Texture = "switch_on.png";
            switchbutton.Tiling = TextureMode.Center;

            ControlStyle checkbox = new ControlStyle(textbox);
            checkbox.CheckedHot.Texture = "border.dds";

            ControlStyle border = new ControlStyle();
            border.Texture = "border.dds";
            border.Tiling = TextureMode.Grid;
            border.Grid = new Squid.Margin(4);

            ControlStyle darkborder = new ControlStyle();
            darkborder.Texture = "border.dds";
            darkborder.Tiling = TextureMode.Grid;
            darkborder.Grid = new Squid.Margin(4);
            darkborder.Tint = ColorInt.ARGB(1f, 0.55f, 0.55f, 0.55f);


            ControlStyle scrollbutton = new ControlStyle(baseStyle);
            scrollbutton.TextAlign = Alignment.MiddleCenter;
            scrollbutton.BackColor = grey30;
            scrollbutton.Hot.BackColor = grey35;
            scrollbutton.Pressed.BackColor = grey35;

            ControlStyle scroll = new ControlStyle(baseStyle);
            scroll.BackColor = grey15;

            var multiline = new ControlStyle(baseStyle);
            multiline.TextPadding = new Squid.Margin(4);
            multiline.TextAlign = Alignment.TopLeft;
            //multiline.BackColor = grey15;

            var dark = new ControlStyle(baseStyle);
            dark.BackColor = grey15;

            var dark2 = new ControlStyle(baseStyle);
            dark2.BackColor = grey10;


            var checkmark = new ControlStyle(baseStyle);
            checkmark.Texture = "checkmark.png";
            checkmark.Tint = ColorInt.ARGB(1f, .8f, .8f, .8f);

            var tile = new ControlStyle(baseStyle);
            tile.BackColor = grey20;
            tile.Hot.BackColor = grey25;

            var viewport = new ControlStyle(baseStyle);
            viewport.Texture = "viewport_border.dds";
            viewport.Grid = new Margin(3);
            viewport.Tiling = TextureMode.Grid;
            viewport.Tint = darkColor;

            var inport = new ControlStyle();
            inport.Tiling = TextureMode.Center;
            inport.Texture = "port.png";
            inport.Hot.Texture = "port_hot.png";
            inport.Selected.Texture = "port_hot.png";
            inport.Pressed.Texture = "port_hot.png";
            inport.SelectedPressed.Texture = "port_hot.png";
            inport.SelectedHot.Texture = "port_hot.png";
            inport.Tint = ColorInt.ARGB(1f, .1f, .5f, .1f);

            var outport = new ControlStyle(inport);
            outport.Tint = ColorInt.ARGB(1f, .5f, .3f, .1f);

            var closebutton = new ControlStyle(button);
            //closebutton.Tint = ColorInt.ARGB(1f, .5f, .3f, .1f);
            closebutton.Texture = "close.dds";
            closebutton.Tiling = TextureMode.Center;

            var closetab = new ControlStyle();
            closetab.Texture = "close.dds";
            closetab.Tint = ColorInt.ARGB(1f, .5f, .5f, .5f);
            closetab.Hot.Tint = ColorInt.ARGB(1f, .75f, .75f, .75f);
            closetab.Tiling = TextureMode.Center;

            var alterRows = new ControlStyle(button);
            alterRows.Texture = "alter_rows.png";
            alterRows.Tiling = TextureMode.RepeatY;

            var iconplus = new ControlStyle();
            iconplus.Texture = "icon_plus.png";
            iconplus.Tint = ColorInt.ARGB(1f, .5f, .5f, .5f);
            iconplus.Hot.Tint = ColorInt.ARGB(1f, .8f, .8f, .8f);

            var foldout = new ControlStyle();
            foldout.Tiling = TextureMode.Center;
            foldout.Texture = "icon_right.png";
            foldout.Checked.Texture = "icon_down.png";

            var canvas = new ControlStyle(frame);
            //canvas.Texture = "checker.dds";
            //canvas.Tiling = TextureMode.Center;


            var prevtoolbutton = new ControlStyle(button);
            prevtoolbutton.TextPadding = new Squid.Margin(4, 0, 4, 0);

            Skin skin = new Skin
            {
                { "prevtoolbutton", prevtoolbutton},
                { "foldout", foldout},
                { "iconplus", iconplus },
                { "canvas", canvas },
                { "window", window },
                { "frame", frame },
                { "label", label },
                { "statusBarLabel", statusBarLabel },
                { "item", item },
                { "button", button },
                { "textbox", textbox },
                { "checkbox", checkbox },
                { "border", border },
                { "darkborder", darkborder },
                { "category", category },
                { "color", colorstyle },
                { "scrollSliderButton", scrollbutton },
                { "tab", tab },
                { "scroll", scroll },
                { "multiline", multiline },
                { "dark", dark },
                { "dark2", dark2 },
                { "checkmark", checkmark },
                { "tile", tile },
                { "node", node },
                { "searchbox", searchbox },
                { "viewport", viewport },
                { "menu", menu },
                { "menuitem", menuitem },
                { "inport", inport },
                { "outport", outport },
                { "close", closebutton },
                { "closetab", closetab },
                { "switch", switchbutton },
                { "dropshadow", dropshadow },
                { "propertyIndent", propertyIndent },
                { "propertyLabel", propertyLabel },
                { "propertyElement", propertyElement },
                { "indent18", indent18 },
                { "alterRows", alterRows },
                { "header", header },
                { "colorGrey170", colorGrey170 },
                { "dropdownLabel", dropdownLabel },
            };

            Point cursorSize = new Point(32, 32);
            Point halfSize = cursorSize / 2;

            CursorSet.Add(Cursors.Default, new Cursor { Texture = "Cursors\\Arrow.png", Size = cursorSize, HotSpot = Point.Zero });
            CursorSet.Add(Cursors.Link, new Cursor { Texture = "Cursors\\Link.png", Size = cursorSize, HotSpot = Point.Zero });
            CursorSet.Add(Cursors.Move, new Cursor { Texture = "Cursors\\Move.png", Size = cursorSize, HotSpot = halfSize });
            CursorSet.Add(Cursors.Select, new Cursor { Texture = "Cursors\\Select.png", Size = cursorSize, HotSpot = halfSize });
            CursorSet.Add(Cursors.SizeNS, new Cursor { Texture = "Cursors\\SizeNS.png", Size = cursorSize, HotSpot = halfSize });
            CursorSet.Add(Cursors.SizeWE, new Cursor { Texture = "Cursors\\SizeWE.png", Size = cursorSize, HotSpot = halfSize });
            CursorSet.Add(Cursors.HSplit, new Cursor { Texture = "Cursors\\SizeNS.png", Size = cursorSize, HotSpot = halfSize });
            CursorSet.Add(Cursors.VSplit, new Cursor { Texture = "Cursors\\SizeWE.png", Size = cursorSize, HotSpot = halfSize });
            CursorSet.Add(Cursors.SizeNESW, new Cursor { Texture = "Cursors\\SizeNESW.png", Size = cursorSize, HotSpot = halfSize });
            CursorSet.Add(Cursors.SizeNWSE, new Cursor { Texture = "Cursors\\SizeNWSE.png", Size = cursorSize, HotSpot = halfSize });

            Skin = skin;

            this.TooltipControl.Style = "frame";
            this.TooltipControl.Padding = new Margin(4);
        }

        public EditorDesktop()
        {
            CreateSkin();

            ModalColor = ColorInt.ARGB(.5f, 0f, 0f, 0f);

            Frame menuBar = new Frame();
            menuBar.Style = "window";
            menuBar.Size = new Point(100, 30);
            menuBar.Dock = DockStyle.Top;
            //frame.Padding = new Margin(1);
            Controls.Add(menuBar);

            DropDownButton btn = CreateMenuItem(menuBar, "File");

            AddMenuItem(btn, "New Scene", (s, a) => { EditorUtility.TextureRoundtripTest(); });
            AddMenuItem(btn, "Open Scene", (s, a) => { EditorUtility.LoadScene(""); });
            var sub = AddMenuItem(btn, "Open Recent", null);
            AddSeparator(btn);
            AddMenuItem(btn, "Save Scene", (s, a) => { EditorUtility.SaveScene(""); });
            AddMenuItem(btn, "Save Scene as...", (s, a) => { EditorUtility.SaveScene(""); });
            AddSeparator(btn);
            AddMenuItem(btn, "Build Settings...", null);
            AddMenuItem(btn, "Build and Run", null);
            AddSeparator(btn);
            AddMenuItem(btn, "Exit", null);

            btn = CreateMenuItem(menuBar, "Edit");
            AddMenuItem(btn, "Undo", null);
            AddMenuItem(btn, "Redo", null);
            AddSeparator(btn);
            AddMenuItem(btn, "Cut", null);
            AddMenuItem(btn, "Copy", null);
            AddMenuItem(btn, "Paste", null);
            AddMenuItem(btn, "Duplicate", null);
            AddSeparator(btn);
            AddMenuItem(btn, "Delete", null);

            btn = CreateMenuItem(menuBar, "Entity");

            AddMenuItem(btn, "Create Empty", null);
            AddSeparator(btn);

            sub = AddMenuItem(btn, "3D Primitive", null);
            AddMenuItem(sub, "Cube", null);
            AddMenuItem(sub, "Sphere", null);
            AddMenuItem(sub, "Capsule", null);
            AddMenuItem(sub, "Cylinder", null);
            AddMenuItem(sub, "Plane", null);
            AddMenuItem(sub, "Quad", null);

            sub = AddMenuItem(btn, "Landscape", null);
            AddMenuItem(sub, "Terrain", null);
            AddMenuItem(sub, "Wind Zone", null);

            sub = AddMenuItem(btn, "Light", null);
            var subsub = AddMenuItem(sub, "Point Light", null);
            AddMenuItem(subsub, "Spot Light", null);
            AddMenuItem(sub, "Spot Light", null);
            AddMenuItem(sub, "Area Light", null);
            AddMenuItem(sub, "Directional Light", null);

            sub = AddMenuItem(btn, "Effects", null);
            AddMenuItem(sub, "Particle Emitter", null);
            AddSeparator(btn);
            sub = AddMenuItem(btn, "Center Camera", null);

            btn = CreateMenuItem(menuBar, "Component");

            var types = Reflector.GetTypes<Component>();
            types.Sort((a,b) => a.Name.CompareTo(b.Name));

            foreach (Type t in types)
                AddMenuItem(btn, t.Name, null);

            btn = CreateMenuItem(menuBar, "Window");

            AddMenuItem(btn, "Scene", (sender, args) => { });
            AddMenuItem(btn, "Explorer", (sender, args) => { });
            AddMenuItem(btn, "Inspector", (sender, args) => {  });
            AddMenuItem(btn, "Project", (sender, args) => { });
            AddMenuItem(btn, "Settings", (sender, args) => { });
            AddMenuItem(btn, "Console", (sender, args) => { });
            //AddMenuItem(btn, "Noise Editor", (sender, args) => NoiseEditor.Show(this));
            //AddMenuItem(btn, "Solarsystem Editor", delegate(Control sender, MouseEventArgs args) { StarGen.Show(this); });
            //AddMenuItem(btn, "Debug", delegate(Control sender) { Inspector.Show(this); });

            btn = CreateMenuItem(menuBar, "Tools");

            AddMenuItem(btn, "Noise Editor", null);
            AddSeparator(btn);
            AddMenuItem(btn, "Merge Splataps", (sender, args) => { EditorUtility.MergeSplatMaps(); });
            AddMenuItem(btn, "test 2", null);
            AddMenuItem(btn, "test 3", null);
            AddMenuItem(btn, "test 4", null);

            statusBar = new Frame
            {
                Style = "window",
                Size = new Point(20, 26),
                Dock = DockStyle.Bottom,
                Parent = this
            };

            var assetBrowser = new Button
            {
                Style = "button",
                Size = new Point(130, 20),
                Dock = DockStyle.Left,
                Margin = new Margin(0, 0, 0, 0),
                Text = "Asset Browser"
            };

            var cmdInput = new Button
            {
                Style = "item",
                Size = new Point(260, 20),
                Dock = DockStyle.Left,
                Margin = new Margin(1, 0, 1, 0),
                Text = "Console"
            };

            var cmdTxt =  new TextBox();
            cmdTxt.Size = new Point(20, 20);
            cmdTxt.Dock = DockStyle.Fill;
            cmdTxt.Style = "textbox";
            cmdTxt.Margin = new Margin(64,2,4,2);
            cmdTxt.KeyDown += Input_KeyDown;
            cmdTxt.TextCommit += Input_OnTextCommit;
            cmdInput.GetElements().Add(cmdTxt);

            var fill = new Frame { Style = "frame", Dock = DockStyle.Fill };

            var fpslabel = new Label
            {
                Style = "statusBarLabel",
                Size = new Point(80, 20),
                Dock = DockStyle.Right,
                Margin = new Margin(1, 0, 0, 0),
            };

            statusBar.Controls.Add(assetBrowser);
            statusBar.Controls.Add(cmdInput);
            statusBar.Controls.Add(fpslabel);
            statusBar.Controls.Add(fill);
            fpslabel.Update += (s) => { fpslabel.Text = $"FPS: {Time.FPS}"; };


            DockArea = new DockRegion { Dock = DockStyle.Fill};
            Scene = DockArea.DockContent("Scene", new ViewportControl());
            var graph = DockArea.DockContent("Graph", new GraphEditor());
            Explorer = DockArea.DockContent(Scene, "Explorer", new ExplorerControl { Size = new Point(480, 300) }, DockStyle.Right);
            Inspector = DockArea.DockContent(Explorer, "Inspector", new InspectorControl(), DockStyle.Bottom);
            Settings = DockArea.DockContent(Inspector, "Settings", new SettingsControl(), DockStyle.Fill);
            Assets = DockArea.DockContent(Scene, "Assets", new AssetsControl(), DockStyle.Bottom);
            Debug = DockArea.DockContent(Assets, "Debug", new DebugControl());
            //var test = DockArea.DockContent(Assets, "VirtualList", new VirtualList());

            //var toolbar = new Frame
            //{
            //   // Style = "frame",
            //    Dock = DockStyle.Left,
            //    Size = new Point(26, 26),
            //    Margin = new Margin(0, 48, 0, 0)
            //};
            //Sceneview.Controls.Insert(0, toolbar);

            Terminal.Command("shapes").OnExecute += TogglePhysxShapes;
            Terminal.Command("aabb").OnExecute += ToggleBoundingBoxes;
            Terminal.Command("stats").OnExecute += PrintStats;
            Terminal.Command("vsync").OnExecute += ChangeVSync;
            Terminal.Command("fps").OnExecute += PrintFPS;

            var toolstrip = new Frame
            {
                Style = "frame",
                Dock = DockStyle.Top,
                Size = new Point(32, 32),
                Margin = new Margin(0, 0, 0, 0)
            };
            Controls.Add(toolstrip);

            //for (int i = 0; i < 6; i++)
            //{
            //    var bt = new Button
            //    {
            //       Size = new Point(20,20),
            //       Dock = DockStyle.Top,
            //       Margin = new Margin(3,0,3,4)
            //    };

            //    toolbar.Controls.Add(bt);
            //}

            Controls.Add(DockArea);

            KeyDown += EditorDesktop_KeyDown;
        }

        void EditorDesktop_KeyDown(Control sender, KeyEventArgs args)
        {
            if(args.Key == Keys.F4)
            {
                RendererSlimDX.Spritebatch.SaveAtlas("D:/Dump/Atlas.png");
            }

            if (args.Key == Keys.DELETE)
            {
                foreach (var entity in Selector.Selection)
                    entity.Destroy();

                Selector.SelectedEntity = null;
                MessageDispatcher.Send(Msg.RefreshExplorer);
            }
        }

        void PrintStats(params string[] values)
        {

        }

        void PrintFPS(params string[] values)
        {
            Spark.Debug.Log($"FPS: { Time.FPS } ");
        }

        void ChangeVSync(params string[] values)
        {
            Engine.Settings.VSync = !Engine.Settings.VSync;
            Spark.Debug.Log("VSync: " + (Engine.Settings.VSync ? "ON" : "OFF"));
        }

        void TogglePhysxShapes(params string[] values)
        {
            Engine.Settings.PhysXShapes = !Engine.Settings.PhysXShapes;
            Spark.Debug.Log("PhysX Shapes: " + (Engine.Settings.PhysXShapes ? "ON" : "OFF"));
        }

        void ToggleBoundingBoxes(params string[] values)
        {
            Engine.Settings.BoundingBoxes = !Engine.Settings.BoundingBoxes;
            Spark.Debug.Log("BoundingBoxes: " + (Engine.Settings.BoundingBoxes ? "ON" : "OFF"));
        }

        void Input_OnTextCommit(object sender, EventArgs e)
        {
            var input = sender as TextBox;
            var text = input.Text;
            
            if (string.IsNullOrEmpty(text))
                return;

            bool result = Terminal.Execute(text);

            if (result)
            {
                if (lastCommands.Count == 0)
                    lastCommands.Insert(0, text);
                else if (text != lastCommands[0])
                    lastCommands.Insert(0, text);

                if (lastCommands.Count > 10)
                    lastCommands.Remove(lastCommands.Last());
            }

            cmdIndex = 0;

            input.Text = string.Empty;
            input.Focus();
        }

        private void Input_KeyDown(Control sender, KeyEventArgs args)
        {
            var input = sender as TextBox;
            if (lastCommands.Count == 0) return;

            if (args.Key == Keys.UPARROW)
            {
                if (cmdIndex == lastCommands.Count) return;

                if (cmdIndex == 0)
                    currentstring = input.Text;

                cmdIndex++;

                if (cmdIndex > lastCommands.Count)
                    cmdIndex = lastCommands.Count;

                input.Text = lastCommands[cmdIndex - 1];
                input.SetCursor(input.Text.Length);
            }
            else if (args.Key == Keys.DOWNARROW)
            {
                if (cmdIndex == 0) return;

                cmdIndex--;

                if (cmdIndex < 0)
                    cmdIndex = 0;

                if (cmdIndex > 0)
                    input.Text = lastCommands[cmdIndex - 1];
                else
                    input.Text = currentstring;

                input.SetCursor(input.Text.Length);
            }
        }
    }
}
