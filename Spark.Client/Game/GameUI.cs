using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spark;
using Squid;
using SharpDX;

namespace Spark.Client
{
    [ExecuteInEditor]
    public class GameUI : Component, IDraw, IUpdate
    {
        public static bool HasMouse
        {
            get
            {
                return EditorUI.MouseCaptured;
                return IMGUI.MouseCaptured;
            }
        }

        public static bool KeyboardCaptured { get { return false; } }

        void LoadScene()
        {
            Editor.LoadScene("");
        }

        void SaveScene()
        {
            Editor.SaveScene("");
        }

        public GameUI()
        {
            // Gui.Renderer = new RendererSlimDX();

            var menuItem = new DropdownItem { name = "File", primary = true };
            menuItem.items.Add(new DropdownItem { name = "New..." });
            menuItem.items.Add(new DropdownItem { name = "Open...", callback = LoadScene });
            menuItem.items.Add(new DropdownItem { name = "Save...", callback = SaveScene });
            menuItem.items.Add(new DropdownItem { name = "Exit" });
            menuItem.items[0].items.Add(new DropdownItem { name = "Nested 1" });
            menu.items.Add(menuItem);

            menuItem = new DropdownItem { name = "Edit", primary = true };
            menuItem.items.Add(new DropdownItem { name = "Undo" });
            menuItem.items.Add(new DropdownItem { name = "Redo" });
            menuItem.items.Add(new DropdownItem { name = " " });
            menuItem.items.Add(new DropdownItem { name = "Cut" });
            menuItem.items.Add(new DropdownItem { name = "Copy" });
            menuItem.items.Add(new DropdownItem { name = "Paste" });
            menuItem.items.Add(new DropdownItem { name = " " });
            menuItem.items.Add(new DropdownItem { name = "Duplicate" });
            menuItem.items.Add(new DropdownItem { name = "Delete" });
            menu.items.Add(menuItem);

            menuItem = new DropdownItem { name = "Assets", primary = true };
            menuItem.items.Add(new DropdownItem { name = "Import" });
            menu.items.Add(menuItem);

            menuItem = new DropdownItem { name = "Entity", primary = true };
            menuItem.items.Add(new DropdownItem { name = "Create Empty" });
            menuItem.items.Add(new DropdownItem { name = "Cube" });
            menuItem.items.Add(new DropdownItem { name = "Sphere" });
            menuItem.items.Add(new DropdownItem { name = "Plane" });
            menuItem.items.Add(new DropdownItem { name = "Mesh" });
            menuItem.items.Add(new DropdownItem { name = "Particles" });
            menuItem.items.Add(new DropdownItem { name = "Terrain" });
            menu.items.Add(menuItem);

            menuItem = new DropdownItem { name = "Component", primary = true };

            var types = Reflector.GetTypes<Component>();
            types.Sort((a, b) => { return a.Name.CompareTo(b.Name); });
            foreach (var type in types)
            {
                menuItem.items.Add(new DropdownItem { name = type.Name });
            }

            //menuItem.items.Add(new DropdownItem { name = "Component 1" });
            //menuItem.items.Add(new DropdownItem { name = "Component 2" });
            //menuItem.items.Add(new DropdownItem { name = "Component 3" });
            //menuItem.items.Add(new DropdownItem { name = "Component 4" });
            menu.items.Add(menuItem);

            menuItem = new DropdownItem { name = "View", primary = true };
            menuItem.items.Add(new DropdownItem { name = "Inspector" });
            menuItem.items.Add(new DropdownItem { name = "Hierarchy" });
            menuItem.items.Add(new DropdownItem { name = "Project" });
            menuItem.items.Add(new DropdownItem { name = "Console" });
            menu.items.Add(menuItem);

            menuItem = new DropdownItem { name = "Help", primary = true };
            menuItem.items.Add(new DropdownItem { name = "Help 1" });
            menuItem.items.Add(new DropdownItem { name = "Help 2" });
            menuItem.items.Add(new DropdownItem { name = "Help 3" });
            menuItem.items.Add(new DropdownItem { name = "Help 4" });
            menu.items.Add(menuItem);
        }

        protected override void Awake()
        {
            Input.OnKeyDown += (key) => { if (key == System.Windows.Forms.Keys.G) Hidden = !Hidden; };
        }

        public void Update()
        {
            Gui.TimeElapsed = Time.DeltaMilliseconds;
            Gui.SetMouse(Input.MousePoint.X, Input.MousePoint.Y, -Input.MouseWheelDelta);
            Gui.SetButtons(Input.IsMouseDown(0), Input.IsMouseDown(1));

            List<KeyData> data = new List<KeyData>();

            foreach (int key in Input.KeysDown)
            {
                int scancode = Input.VirtualKeyToScancode(key);
                data.Add(new KeyData { Pressed = true, Scancode = scancode, Char = Input.ScancodeToChar(scancode) });
            }

            foreach (int key in Input.KeysReleased)
            {
                int scancode = Input.VirtualKeyToScancode(key);
                data.Add(new KeyData { Pressed = false, Scancode = scancode, Char = Input.ScancodeToChar(scancode) });
            }

            //if (Input.Alt)
            //    data.Add(new KeyData { Pressed = true, Scancode = Input.VirtualKeyToScancode((int)System.Windows.Forms.Keys.Alt) });

            Gui.SetKeyboard(data.ToArray());

        }

        public bool Hidden;

        public void Draw()
        {
            CommandBuffer.Enqueue(RenderPass.Overlay, DrawCallback);
        }

        private GUIWindow project = new GUIWindow { tabs = { new ProjectWindow { name = "project" }, new EditorConsoleWindow { name = "debug" } } };
        private GUIWindow inspector = new GUIWindow { tabs = { new InspectorWindow { name = "inspector" }, new DebugWindow { name = "debug" } } };
        private GUIWindow hierarchy = new GUIWindow { tabs = { new HierarchyWindow { name = "hierarchy" } } };
        private GUIMenu menu = new GUIMenu();

        private void DrawCallback()
        {
            //Engine.Device.ImmediateContext.Rasterizer.State = States.Scissor;
            Engine.Device.ImmediateContext.OutputMerger.SetDepthStencilState(States.ZReadNoZWriteNoStencil, 0);

            inspector.rect = new Rect((int)RenderView.Active.Size.X - 300, 34, 300, (int)RenderView.Active.Size.Y - 34);
            hierarchy.rect = new Rect(0, 34, 300, (int)RenderView.Active.Size.Y - 34);
            project.rect = new Rect(300, (int)RenderView.Active.Size.Y - 300, (int)RenderView.Active.Size.X - 600, 300);

            IMGUI.Begin(Input.MousePoint.X, Input.MousePoint.Y, Input.IsMouseDown(0) ? 1 : 0, -Input.MouseWheelDelta / 100);

            if (!Hidden)
                hierarchy.OnGUI();
            inspector.OnGUI();

            if (!Hidden)
                project.OnGUI();

            menu.OnGUI();

           // IMGUI.Texture(new Rect(200, 32, 200, 200), DeferredRenderer.Current.ShadowBuffer, -1);

            IMGUI.End();
            IMGUI.Draw();
        }
    }
}
