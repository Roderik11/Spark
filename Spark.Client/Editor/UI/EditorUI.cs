using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Squid;
using Spark;
using System.Reflection;
using System.Deployment.Application;
using System.Runtime.InteropServices;

namespace Spark.Client
{
    public class EditorUI : EditorDesktop
    {
        private static Desktop staticdesk;
        private KeyData[] keyArray = new KeyData[64];

        public static bool KeyboardCaptured
        {
            get
            {
                if (staticdesk == null) return false;
                return staticdesk.FocusedControl != null;
            }
        }

        public static bool MouseCaptured
        {
            get
            {
                if (staticdesk == null) return false;
                if (staticdesk.HotControl is InnerViewport)
                    return false;
                return staticdesk.HotControl != staticdesk || staticdesk.PressedControl != null;
            }
        }

        public static bool Modal => staticdesk.IsModal;

        public EditorUI()
        {
            staticdesk = this;
            ShowCursor = true;
        }

        public new void Update()
        {
            Gui.TimeElapsed = Time.DeltaMilliseconds;
            Gui.SetMouse(Input.MousePoint.X, Input.MousePoint.Y, -Input.MouseWheelDelta);
            Gui.SetButtons(Input.IsMouseDown(0), Input.IsMouseDown(1), Input.IsMouseDown(2));

            int total = Input.KeysDown.Count + Input.KeysReleased.Count;

            if(total > keyArray.Length)
                Array.Resize(ref keyArray, total);
            
            int index = 0;

            for (int i = 0; i < Input.KeysDown.Count; i++)
            {
                var key = (int)Input.KeysDown[i];
                int scancode = Input.VirtualKeyToScancode(key);
                keyArray[index] = new KeyData
                {
                    Pressed = true,
                    Scancode = scancode,
                    Char = Input.ScancodeToChar(scancode)

                };

                index++;
            }

            for (int i = 0; i < Input.KeysReleased.Count; i++)
            {
                var key = (int)Input.KeysReleased[i];
                int scancode = Input.VirtualKeyToScancode(key);
                keyArray[index] = new KeyData
                {
                    Pressed = false,
                    Scancode = scancode,
                    Char = Input.ScancodeToChar(scancode)
                };
            
                index++;
            }

            //if (Input.Alt)
            //    data.Add(new KeyData { Pressed = true, Scancode = Input.VirtualKeyToScancode((int)System.Windows.Forms.Keys.Alt) });

            Gui.SetKeyboard(keyArray, index);

            Desktop.Size = new Squid.Point((int)RenderView.Active.Size.X, (int)RenderView.Active.Size.Y);

            Profiler.Start("GUI.Update");
            base.Update();
            Profiler.Stop();
        }

        public new void Draw()
        {
            Profiler.Start("GUI.Draw");
            base.Draw();
            Profiler.Stop();
        }
    }
}
