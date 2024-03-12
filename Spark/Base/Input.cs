using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using SharpDX.Multimedia;
using SharpDX.RawInput;

namespace Spark
{
    public static class Input
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }

            public static implicit operator System.Drawing.Point(POINT p)
            {
                return new System.Drawing.Point(p.X, p.Y);
            }

            public static implicit operator POINT(System.Drawing.Point p)
            {
                return new POINT(p.X, p.Y);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int X;
            public int Y;
            public int W;
            public int H;

            public RECT(int x, int y, int w, int h)
            {
                this.X = x;
                this.Y = y;
                this.W = w;
                this.H = h;
            }
        }

        [DllImport("user32.dll")]
        private static extern int GetKeyboardLayout(int dwLayout);

        [DllImport("user32.dll")]
        private static extern int GetKeyboardState(ref byte pbKeyState);

        [DllImport("user32.dll", EntryPoint = "MapVirtualKeyEx")]
        private static extern int MapVirtualKeyExA(int uCode, int uMapType, int dwhkl);

        [DllImport("user32.dll")]
        private static extern int ToAsciiEx(int uVirtKey, int uScanCode, ref byte lpKeyState, ref short lpChar, int uFlags, int dwhkl);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int ToUnicodeEx(int virtualKeyCode, int scanCode, ref byte keyboardState, [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] StringBuilder receivingBuffer, int bufferSize, uint flags, int dwhkl);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, ref RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool PtInRect([In] ref RECT lprc, POINT pt);

        private static int KeyboardLayout;
        private static byte[] KeyStates;
        private static bool[] Buttons;

        public static List<Keys> KeysPressed { get; private set; }
        public static List<Keys> KeysReleased { get; private set; }
        public static List<Keys> KeysDown { get; private set; }

        public static List<Keys> KeysDown2 { get; private set; }

        public static Point MouseCursor { get; private set; }
        public static Point MousePoint { get; private set; }
        public static Point MouseDelta { get; private set; }
        public static int MouseWheelDelta { get; private set; }

        private static Dictionary<System.Windows.Forms.Keys, int> SpecialKeys = new Dictionary<System.Windows.Forms.Keys, int>();

        public static bool Shift;
        public static bool Alt;

        public static bool MouseOutside { get; private set; }

        private static IntPtr Handle;

        static Input()
        {
            Buttons = new bool[8];
            KeysDown = new List<Keys>();
            KeysDown2 = new List<Keys>();
            KeysPressed = new List<Keys>();
            KeysReleased = new List<Keys>();
            KeyboardLayout = GetKeyboardLayout(0);
            KeyStates = new byte[0x100];

            SpecialKeys.Add(System.Windows.Forms.Keys.Home, 0xC7);
            SpecialKeys.Add(System.Windows.Forms.Keys.Up, 0xC8);
            SpecialKeys.Add(System.Windows.Forms.Keys.Left, 0xCB);
            SpecialKeys.Add(System.Windows.Forms.Keys.Right, 0xCD);
            SpecialKeys.Add(System.Windows.Forms.Keys.End, 0xCF);
            SpecialKeys.Add(System.Windows.Forms.Keys.Down, 0xD0);
            SpecialKeys.Add(System.Windows.Forms.Keys.Insert, 0xD2);
            SpecialKeys.Add(System.Windows.Forms.Keys.Delete, 0xD3);
            SpecialKeys.Add(System.Windows.Forms.Keys.MediaPreviousTrack, 0x90);
            SpecialKeys.Add(System.Windows.Forms.Keys.NumPad0, 11);
            SpecialKeys.Add(System.Windows.Forms.Keys.NumPad1, 2);
            SpecialKeys.Add(System.Windows.Forms.Keys.NumPad2, 3);
            SpecialKeys.Add(System.Windows.Forms.Keys.NumPad3, 4);
            SpecialKeys.Add(System.Windows.Forms.Keys.NumPad4, 5);
            SpecialKeys.Add(System.Windows.Forms.Keys.NumPad5, 6);
            SpecialKeys.Add(System.Windows.Forms.Keys.NumPad6, 7);
            SpecialKeys.Add(System.Windows.Forms.Keys.NumPad7, 8);
            SpecialKeys.Add(System.Windows.Forms.Keys.NumPad8, 9);
            SpecialKeys.Add(System.Windows.Forms.Keys.NumPad9, 10);

            Device.RegisterDevice(UsagePage.Generic, UsageId.GenericKeyboard, DeviceFlags.None);
            Device.RegisterDevice(UsagePage.Generic, UsageId.GenericMouse, DeviceFlags.None);

            Device.MouseInput += Device_MouseInput;
            Device.KeyboardInput += Device_KeyboardInput;
        }

        public static void SetHandle(IntPtr handle)
        {
            Handle = handle;
            POINT p = MouseCursor;
            ScreenToClient(Handle, ref p);
            RECT rect = new RECT();
            GetClientRect(Handle, ref rect);
            MouseOutside = !PtInRect(ref rect, p);
            MousePoint = p;
        }

        private static void Device_MouseInput(object sender, MouseInputEventArgs e)
        {
            MouseDelta = new Point(e.X, e.Y);
            MouseWheelDelta = e.WheelDelta;
            MouseCursor = Cursor.Position;

            if (Handle != IntPtr.Zero)
            {
                POINT p = MouseCursor;
                ScreenToClient(Handle, ref p);
                RECT rect = new RECT();
                GetClientRect(Handle, ref rect);
                MouseOutside = !PtInRect(ref rect, p);
                MousePoint = p;
            }
            else
            {
                MousePoint = MouseCursor;
            }

            if (e.ButtonFlags != MouseButtonFlags.None)
            {
                switch (e.ButtonFlags)
                {
                    case MouseButtonFlags.Button1Down | MouseButtonFlags.LeftButtonDown:
                        Buttons[0] = true;
                        break;

                    case MouseButtonFlags.Button1Up | MouseButtonFlags.LeftButtonUp:
                        Buttons[0] = false;
                        break;

                    case MouseButtonFlags.Button2Down | MouseButtonFlags.RightButtonDown:
                        Buttons[1] = true;
                        break;

                    case MouseButtonFlags.Button2Up | MouseButtonFlags.RightButtonUp:
                        Buttons[1] = false;
                        break;

                    case MouseButtonFlags.Button3Down | MouseButtonFlags.MiddleButtonDown:
                        Buttons[2] = true;
                        break;

                    case MouseButtonFlags.Button3Up | MouseButtonFlags.MiddleButtonUp:
                        Buttons[2] = false;
                        break;

                    case MouseButtonFlags.Button4Down:
                        Buttons[3] = true;
                        break;

                    case MouseButtonFlags.Button4Up:
                        Buttons[3] = false;
                        break;

                    case MouseButtonFlags.Button5Down:
                        Buttons[4] = true;
                        break;

                    case MouseButtonFlags.Button5Up:
                        Buttons[4] = false;
                        break;
                }
            }
        }

        private static void Device_KeyboardInput(object sender, SharpDX.RawInput.KeyboardInputEventArgs e)
        {
            if (e.State == KeyState.KeyDown || e.State == KeyState.SystemKeyDown)
            {
                if (e.Key == Keys.ShiftKey)
                    Shift = true;

                if (e.Key == (Keys.RButton | Keys.ShiftKey))
                    Alt = true;

                Keys key = e.Key & ~Keys.Shift;

                if (!KeysDown2.Contains(key))
                    KeysDown2.Add(key);

                if (!KeysDown.Contains(key))
                {
                    KeysDown.Add(key);
                }
                else if (!KeysPressed.Contains(key))
                {
                    KeysPressed.Add(key);
                }
            }
            else if (e.State == KeyState.KeyUp || e.State == KeyState.SystemKeyDown)
            {
                if (e.Key == Keys.ShiftKey) Shift = false;
                if (e.Key == (Keys.RButton | Keys.ShiftKey))
                    Alt = false;

                Keys key = e.Key & ~Keys.Shift;

                KeysReleased.Add(key);
                KeysDown.Remove(key);
                KeysPressed.Remove(key);
                KeysDown2.Remove(key);
            }
        }

        public static char? ScancodeToChar(int code)
        {
            short lpChar = 0;
            if (GetKeyboardState(ref KeyStates[0]) == 0)
                return null;

            var buf = new StringBuilder(4);
            //int result = ToAsciiEx(MapVirtualKeyExA(code, 1, KeyboardLayout), code, ref KeyStates[0], ref lpChar, 0, KeyboardLayout);
            int result = ToUnicodeEx(MapVirtualKeyExA(code, 1, KeyboardLayout), code, ref KeyStates[0], buf, 4, 0, KeyboardLayout);

            if (result == 1)
            {
                return buf[0];
                //return (char?)((ushort)lpChar);
            }

            return null;
        }

        public static int VirtualKeyToScancode(int key)
        {
            if (SpecialKeys.ContainsKey((Keys)key))
                return SpecialKeys[(Keys)key];

            var result = MapVirtualKeyExA(key, 0, KeyboardLayout);
            return result;
        }

        public static void ClearKeyCache()
        {
            foreach (var key in KeysDown)
                OnKeyDown?.Invoke(key);

            KeysDown.Clear();
            KeysPressed.Clear();
            KeysReleased.Clear();
            MouseDelta = new Point();
            MouseWheelDelta = 0;
        }

        public static event Action<Keys> OnKeyDown;

        public static bool IsMouseDown(int button)
        {
            if (button > Buttons.Length) return false;
            return Buttons[button];
        }

        public static bool IsKeyDown(Keys key)
        {
            return KeysDown2.Contains(key);
        }

        public static bool IsKey(Keys key)
        {
            return KeysDown.Contains(key);
        }
    }
}