using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using SharpDX;

namespace Spark.Client
{
    public struct Vertex2
    {
        public float x;
        public float y;
        public uint color;
    }

    public class Shape2D
    {
        public List<Vertex2> Vertices = new List<Vertex2>();

        public void Vertex(float x, float y, int color)
        {
            uint c = ColorToUInt(color);
            Vertex2 v = new Vertex2();
            v.x = x;
            v.y = y;
            v.color = ColorToUInt(color);
            Vertices.Add(v);
        }

        private uint ColorToUInt(int col)
        {
            System.Drawing.Color color = System.Drawing.Color.FromArgb(col);
            return (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | (color.B << 0));
        }
    }

    public enum IMGUICommandType
    {
        Rect,
        Triangle,
        Text,
        Texture,
        Scissor
    }

    public class GuiStyle
    {
        public int MinHeight = 20;
        public TextAlign TextAlign = TextAlign.Left;
    }

    public struct Rect
    {
        public int x, y, w, h;

        public Rect(int x, int y, int w, int h)
        {
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
        }

        public bool Intersects(Rect rect)
        {
            if (x > rect.x + rect.w) return false;
            if (x + w < rect.x) return false;
            if (y > rect.y + rect.h) return false;
            if (y + h < rect.y) return false;

            return true;
        }

        public Rect Clip(Rect rect)
        {
            Rect result = new Rect();
            result.x = Math.Max(x, rect.x);
            result.y = Math.Max(y, rect.y);
            result.w = Math.Min(x + w, rect.x + rect.w) - result.x;
            result.h = Math.Min(y + h, rect.y + rect.h) - result.y;
            return result;
        }

        public bool Contains(System.Drawing.Point point)
        {
            return point.X >= x && point.X <= x + w && point.Y >= y && point.Y <= y + h;
        }
    }

    public struct IMGUIText
    {
        public int x, y;
        public string Text;
        public int Font;
    }

    public struct IMGUICommand
    {
        public IMGUICommandType Type;
        public int Flags;
        public int Color;
        public Texture Texture;
        public Rect Rect;
        public IMGUIText Text;
    }

    public enum MouseButton
    {
        Left = 0x01,
        Right = 0x02,
    }

    public enum TextAlign
    {
        Left,
        Center,
        Right,
    }

    public enum Grouping
    {
        None,
        Vertical,
        Horizontal,
    }

    public class GuiContent
    {
        public string text;
        public int texture;

        public static implicit operator GuiContent(string text)
        {
            return new GuiContent { text = text };
        }

        public static implicit operator GuiContent(int texture)
        {
            return new GuiContent { texture = texture };
        }
    }

    public struct IMGUIState
    {
        public bool left;
        public bool leftPressed, leftReleased;
        public int mx, my;
        public int scroll;
        public uint active;
        public uint focus;
        public uint hot;
        public uint hotToBe;
        public bool isHot;
        public bool isActive;
        public bool wentActive;
        public int dragX, dragY;
        public float dragOrig;
        public int widgetX, widgetY, widgetW, widgetH;
        public bool insideCurrentScroll;
        public bool caret;
        public uint areaId;
        public uint widgetId;
    }

    public struct ScrollState
    {
        public bool Active;
        public int Top;
        public int Bottom;
        public int Right;
        public int Left;
        public int AreaTop;
        public uint ID;
        public bool Inside;
        public int Value;
    }

    public static class IMGUI
    {
        public static List<IMGUICommand> Commands = new List<IMGUICommand>();
        public static bool MouseCaptured { get; private set; }

        private static Stack<bool> groupingstack = new Stack<bool>();

        #region constants

        public static int DEFAULT_SPACING = 4;
        public static int BUTTON_HEIGHT = 22;
        static int BUTTON_WIDTH = 120;
        static int SLIDER_HEIGHT = 24;
        static int SLIDER_MARKER_WIDTH = 16;
        static int CHECK_SIZE = 8;
        static int TEXT_HEIGHT = 12;
        static int AREA_PADDING = 4;
        static int INDENT_SIZE = 8;
        static int SCROLLBAR = 8;

        #endregion

        #region private fields

        static IMGUIState State;
        static ScrollState Scroll;
        public static int Font = 0;

        #endregion

        #region private methods

        static bool anyActive()
        {
            return State.active != 0;
        }

        static bool isFocus(uint id)
        {
            return State.focus == id;
        }

        static bool isActive(uint id)
        {
            return State.active == id;
        }

        static bool isHot(uint id)
        {
            return State.hot == id;
        }

        static bool inRect(int x, int y, int w, int h, bool checkScroll)
        {
            return (!checkScroll || State.insideCurrentScroll) && State.mx >= x && State.mx <= x + w && State.my >= y && State.my <= y + h;
        }

        static void clearInput()
        {
            State.leftPressed = false;
            State.leftReleased = false;
            State.scroll = 0;
        }

        static void clearActive()
        {
            State.active = 0;
            // mark all UI for this frame as processed
            clearInput();
        }

        static void setActive(uint id)
        {
            State.active = id;
            State.wentActive = true;
        }

        static void setHot(uint id)
        {
            State.hotToBe = id;
        }

        static bool buttonLogic(uint id, bool over)
        {
            bool result = false;
            // process down
            if (!anyActive())
            {
                if (over)
                    setHot(id);

                if (isHot(id) && State.leftPressed)
                {
                    setActive(id);
                    MouseCaptured = true;
                }
            }

            // if button is active, then react on left up
            if (isActive(id))
            {
                State.isActive = true;

                if (over)
                    setHot(id);

                if (State.leftReleased)
                {
                    if (isHot(id))
                        result = true;

                    clearActive();
                }
            }

            if (isHot(id))
                State.isHot = true;

            if (result)
                State.focus = id;

            return result;
        }

        static void updateInput(int mx, int my, int mbut, int scroll)
        {
            bool left = (mbut & (int)MouseButton.Left) != 0;

            State.mx = mx;
            State.my = my;
            State.leftPressed = !State.left && left;
            State.leftReleased = State.left && !left;
            State.left = left;
            State.scroll = scroll;
        }

        #endregion

        #region public methods

        static IMGUI()
        {
            int font = renderer.GetFont("default");
            SetFont(font);
        }

        public static void SetFont(int font)
        {
            if (Font == font) return;
            Font = font;
            Point v = GetTextSize("W", font);
            TEXT_HEIGHT = v.Y;
        }

        public static void BeginVertical()
        {
            horizontal = false;
            groupingstack.Push(horizontal);
        }

        public static void EndVertical()
        {
            if (horizontal) return;
            groupingstack.Pop();

            if (groupingstack.Count > 0)
                horizontal = groupingstack.Peek();
            else
                horizontal = false;
        }

        public static void BeginHorizontal()
        {
            horizontal = true;
            groupingstack.Push(horizontal);
        }

        public static void EndHorizontal()
        {
            if (!horizontal) return;
            groupingstack.Pop();

            if (groupingstack.Count > 0)
                horizontal = groupingstack.Peek();
            else
                horizontal = false;
        }

        public static void Begin(int mx, int my, int mbut, int scroll)
        {
            updateInput(mx, my, mbut, scroll);

            MouseCaptured = false;

            State.hot = State.hotToBe;
            State.hotToBe = 0;

            State.wentActive = false;
            State.isActive = false;
            State.isHot = false;

            State.widgetX = 0;
            State.widgetY = 0;
            State.widgetW = 0;
            State.widgetH = 0;

            State.areaId = 1;
            State.widgetId = 1;

            if (State.focus != 0 && State.leftPressed)
                State.focus = 0;

            Commands.Clear();
        }

        public static void End()
        {
            clearInput();
        }

        public static bool BeginArea(int x, int y, int w, int h, int scroll, int color = -1777399284)
        {
            Changed = false;

            State.areaId++;
            State.widgetId = 0;
            Scroll.ID = (State.areaId << 16) | State.widgetId;

            State.widgetX = x + AREA_PADDING;
            State.widgetY = y + AREA_PADDING - scroll;
            State.widgetW = w - AREA_PADDING * 2 - SCROLLBAR;
            State.widgetH = h - AREA_PADDING * 2 + scroll;

            Scroll.Active = true;
            Scroll.Top = y;
            Scroll.Bottom = y + h - AREA_PADDING;
            Scroll.Left = x + AREA_PADDING;
            Scroll.Right = x + w - SCROLLBAR - AREA_PADDING;
            Scroll.Value = scroll;
            Scroll.AreaTop = State.widgetY;
            Scroll.Inside = inRect(x, y, w, h, false);

            State.insideCurrentScroll = Scroll.Inside;

            CmdRectangle(x, y, w, h, color);
            CmdScissor(x + AREA_PADDING, y + AREA_PADDING, w - AREA_PADDING * 2, h - AREA_PADDING * 2);

            return Scroll.Inside;
        }

        public static void BeginGroup(int x, int y, int w, int h, int color = -1777399284)
        {
            Changed = false;

            State.areaId++;
            State.widgetId = 0;
            Scroll.ID = (State.areaId << 16) | State.widgetId;

            State.widgetX = x;
            State.widgetY = y;
            State.widgetW = w;
            State.widgetH = h;

            bool over = inRect(x, y, w, h, false);
            bool res = buttonLogic(Scroll.ID, over);

            Scroll.Active = false;
            Scroll.Top = y;
            Scroll.Bottom = y + h;
            Scroll.Right = x + w - AREA_PADDING * 3;
            Scroll.Left = x + AREA_PADDING;
            Scroll.AreaTop = State.widgetY;
            Scroll.Inside = inRect(x, y, w, h, false);
            Scroll.Value = 0;
            State.insideCurrentScroll = Scroll.Inside;

            CmdRectangle(x, y, w, h, color);
            CmdScissor(x, y, w, h);
        }

        public static void EndGroup()
        {
            Changed = false;

            // Disable scissoring.
            CmdScissor(-1, -1, -1, -1);
        }

        public static int EndArea()
        {
            Changed = false;

            // Disable scissoring.
            CmdScissor(-1, -1, -1, -1);

            if (Scroll.Active)
            {
                // Draw scroll bar
                int x = Scroll.Right;
                int y = Scroll.Top;
                int w = SCROLLBAR;
                int h = Scroll.Bottom - Scroll.Top;

                int stop = Scroll.AreaTop;
                int sbot = State.widgetY;
                int sh = sbot - stop; // The scrollable area height.

                float barHeight = (float)h / (float)sh;

                if (barHeight < 1)
                {
                    // Handle scroll bar logic.
                    uint hid = Scroll.ID;
                    int hx = x;
                    float s = (float)Scroll.Value / (float)(sh - h);

                    int hh = (int)(barHeight * h);
                    hh = Math.Max(32, hh);

                    int hy = y + (int)(s * (h - hh));
                    int hw = w;

                    int range = h - (hh - 1);
                    bool over = inRect(hx, hy, hw, hh, true);
                    buttonLogic(hid, over);
                    if (isActive(hid))
                    {
                        float u = (float)(hy - y) / (float)range;
                        if (State.wentActive)
                        {
                            State.dragY = State.my;
                            State.dragOrig = u;
                        }
                        if (State.dragY != State.my)
                        {
                            u = State.dragOrig + (State.my - State.dragY) / (float)range;
                            if (u < 0) u = 0;
                            if (u > 1) u = 1;
                            Scroll.Value = (int)((u) * (sh - h));
                        }
                    }

                    // BG
                    CmdRectangle(x, y, w, h, RGBA(0, 0, 0, 196));
                    // Bar
                    if (isActive(hid))
                        CmdRectangle(hx, hy, hw, hh, RGBA(255, 196, 0, 196));
                    else
                        CmdRectangle(hx, hy, hw, hh, isHot(hid) ? RGBA(255, 196, 0, 96) : RGBA(255, 255, 255, 64));

                    // Handle mouse scrolling.
                    if (Scroll.Inside) // && !anyActive())
                    {
                        if (State.scroll != 0)
                        {
                            Scroll.Value += 20 * State.scroll;
                            if (Scroll.Value < 0) Scroll.Value = 0;
                            if (Scroll.Value > (sh - h)) Scroll.Value = (sh - h);
                        }
                    }
                }
            }

            if (!MouseCaptured) MouseCaptured = State.isHot || State.isActive || State.insideCurrentScroll || Scroll.Inside;

            State.insideCurrentScroll = false;
            return Scroll.Value;
        }

        static bool horizontal = false;

        public static bool Button(string text, bool enabled)
        {
            Rect rect = GetNextRect();
            return Button(rect, text, enabled);

            //Changed = false;

            //State.widgetId++;
            //uint id = (State.areaId << 16) | State.widgetId;


            //int x = rect.x;// State.widgetX;
            //int y = rect.y;//State.widgetY;
            //int w = rect.w;//horizontal ? BUTTON_WIDTH : State.widgetW;
            //int h = rect.h;//BUTTON_HEIGHT;

            //// Rect rect = new Rect(x, y, w, h);
            //Point p = AlignText(rect, text, Font, TextAlign.Left);

            //bool over = enabled && inRect(x, y, w, h, true);
            //bool result = buttonLogic(id, over);
            //if (result) Changed = result;

            //if (isActive(id))
            //    CmdRectangle(x, y, w, h, RGBA(255, 196, 0, 196));
            //else
            //    CmdRectangle(x, y, w, h, isHot(id) ? RGBA(255, 196, 0, 96) : RGBA(255, 255, 255, 64));

            //if (enabled)
            //    CmdText(p.X, p.Y, text, RGBA(255, 255, 255, 200));
            //else
            //    CmdText(p.X, p.Y, text, RGBA(128, 128, 128, 200));

            //if(horizontal)
            //    State.widgetX += BUTTON_WIDTH + DEFAULT_SPACING;
            //else
            //    State.widgetY += BUTTON_HEIGHT + DEFAULT_SPACING;

            //return result;
        }

        public static bool Button(Rect rect, string text, bool enabled)
        {
            Changed = false;

            State.widgetId++;
            uint id = (State.areaId << 16) | State.widgetId;

            int x = rect.x;
            int y = rect.y;
            int w = rect.w;
            int h = rect.h;

            Point p = AlignText(rect, text, Font, TextAlign.Left);

            bool over = enabled && inRect(x, y, w, h, false);
            bool res = buttonLogic(id, over);
            if (res) Changed = res;

            if (isActive(id))
                CmdRectangle(x, y, w, h, RGBA(255, 196, 0, 196));
            else
                CmdRectangle(x, y, w, h, isHot(id) ? RGBA(255, 196, 0, 96) : RGBA(255, 255, 255, 64));

            if (enabled)
                CmdText(p.X, p.Y, text, RGBA(255, 255, 255, 200));
            else
                CmdText(p.X, p.Y, text, RGBA(128, 128, 128, 200));

            return res;
        }

        public static int Skip(string text, int min, int max, int val, bool enabled)
        {
            Changed = false;

            State.widgetId++;
            uint id = (State.areaId << 16) | State.widgetId;

            int x = State.widgetX;
            int y = State.widgetY;
            int w = State.widgetW;
            int h = BUTTON_HEIGHT;

            int bsize = w / 4;

            val = Math.Min(Math.Max(val, min), max);
            int temp = val;

            if (Button(new Rect(x, y, bsize, h), "<<", val > min))
                val--;

            Label(new Rect(x + bsize + 4, y, w - bsize * 2 - 8, h), text);

            if (Button(new Rect(x + w - bsize, y, bsize, h), ">>", val < max))
                val++;

            val = Math.Min(Math.Max(val, min), max);
            Changed = val != temp;

            State.widgetY += BUTTON_HEIGHT + DEFAULT_SPACING;

            return val;
        }

        public static string Textbox(Rect rect, string text, int maxlen, bool enabled = true)
        {
            if (text == null)
                text = string.Empty;

            Changed = false;

            State.widgetId++;
            uint id = (State.areaId << 16) | State.widgetId;

            int x = rect.x;
            int y = rect.y;
            int w = rect.w;
            int h = rect.h;

            Point p = AlignText(rect, text, Font, TextAlign.Left);

            bool over = enabled && inRect(x, y, w, h, true);
            bool res = buttonLogic(id, over);

            if (isFocus(id))
            {
                foreach (var key in Input.KeysDown)
                {
                    if (key == System.Windows.Forms.Keys.Back && text.Length > 0)
                        text = text.Substring(0, text.Length - 1);
                    else if (key == System.Windows.Forms.Keys.Return)
                        State.focus = 0;
                    else if (text.Length < maxlen || maxlen < 1)
                    {
                        if (key == System.Windows.Forms.Keys.Back)
                            continue;

                        int scancode = Input.VirtualKeyToScancode((int)key);
                        char? ch = Input.ScancodeToChar(scancode);

                        if (ch.HasValue)
                            text += ch.Value.ToString();
                    }
                }
            }

            if (isFocus(id))
                CmdRectangle(x, y, w, h, RGBA(255, 196, 0, 96));
            else
                CmdRectangle(x, y, w, h, RGBA(128, 128, 128, (byte)(isActive(id) ? 196 : 96)));

            Rect lastscissor = IMGUI.CurrentScissor;
            var clipped = rect.Clip(lastscissor);

            CmdScissor(clipped.x, clipped.y, clipped.w, clipped.h);

            if (enabled)
                CmdText(p.X + 2 , p.Y, text, RGBA(255, 255, 255, 200));
            else
                CmdText(p.X + 2, p.Y, text, RGBA(128, 128, 128, 200));

            CmdScissor(lastscissor.x, lastscissor.y, lastscissor.w, lastscissor.h);

            if (isFocus(id))
            {
                Point size = GetTextSize(text, Font);
                CmdRectangle(p.X + 4 + size.X, p.Y, 1, size.Y, -1);
            }

            return text;
        }

        public static string Textbox(string text, int maxlen, bool enabled = true)
        {
            if (text == null)
                text = string.Empty;

            int x = State.widgetX;
            int y = State.widgetY;
            int w = State.widgetW;
            int h = BUTTON_HEIGHT;

            var rect = new Rect(x, y, w, h);

            State.widgetY += BUTTON_HEIGHT + DEFAULT_SPACING;

            return Textbox(rect, text, maxlen, enabled);
        }

        public static void Progress(string text, float val)
        {
            Changed = false;

            State.widgetId++;
            uint id = (State.areaId << 16) | State.widgetId;

            int x = State.widgetX;
            int y = State.widgetY;
            int w = State.widgetW;
            int h = BUTTON_HEIGHT;

            Point p = AlignText(new Rect(x, y, w, h), text, Font, TextAlign.Left);

            bool over = inRect(x, y, w, h, true);
            bool res = buttonLogic(id, over);

            // BG
            CmdRectangle(x, y, w, h, RGBA(128, 128, 128, 96));

            int w2 = (int)(float)(w * val);

            // Bar
            CmdRectangle(x, y, w2, h, RGBA(255, 196, 0, 96));

            // text
            CmdText(p.X + BUTTON_HEIGHT / 2, p.Y, text, RGBA(255, 255, 255, 200));

            State.widgetY += BUTTON_HEIGHT + DEFAULT_SPACING;
        }

        public static bool Item(string text, bool enabled = true)
        {
            var size = GetTextSize(text, Font);
            var rect = GetNextRect(size);
            return Item(rect, text, enabled);
        }

        public static bool Item(Rect rect, string text, bool enabled = true)
        {
            Changed = false;

            State.widgetId++;
            uint id = (State.areaId << 16) | State.widgetId;

            var size = GetTextSize(text, Font);
            Point p = AlignText(rect, size, TextAlign.Left);

            int x = rect.x;
            int y = rect.y;
            int w = rect.w;
            int h = rect.h;
           
            bool over = enabled && inRect(x, y, w, h, true);
            bool res = buttonLogic(id, over);
            if (res) Changed = res;

            if (isHot(id))
                CmdRectangle(x, y, w, h, RGBA(255, 196, 0, (byte)(isActive(id) ? 196 : 96)));

            int color = enabled ? RGBA(255, 255, 255, 200) : RGBA(128, 128, 128, 200);
            CmdText(p.X, p.Y, text, size, color);

            return res;
        }

        public static bool FoldOut(string text, bool expanded, out bool clicked, bool enabled = true)
        {
            var size = GetTextSize(text, Font);
            var rect = GetNextRect(size);
            return FoldOut(rect, text, expanded, out clicked, enabled);
        }

        public static bool FoldOut(Rect rect, string text, bool expanded, out bool clicked, bool enabled = true)
        {
            Changed = false;

            var left = new Rect(rect.x, rect.y, 16, rect.h);
            rect.x += 16;rect.w -= 16;

            if (Button(left, "", enabled))
                expanded = !expanded;

            State.widgetId++;
            uint id = (State.areaId << 16) | State.widgetId;

            var size = GetTextSize(text, Font);
            Point p = AlignText(rect, size, TextAlign.Left);

            int x = rect.x;
            int y = rect.y;
            int w = rect.w;
            int h = rect.h;

            bool over = enabled && inRect(x, y, w, h, true);
            bool res = buttonLogic(id, over);
            if (res) Changed = res;

            if (isHot(id))
                CmdRectangle(x, y, w, h, RGBA(255, 196, 0, (byte)(isActive(id) ? 196 : 96)));

            int color = enabled ? RGBA(255, 255, 255, 200) : RGBA(128, 128, 128, 200);
            CmdText(p.X, p.Y, text, size, color);

            clicked = res;
            return expanded;
        }


        public static void Texture(Rect rect, Texture texture, int color)
        {
            CmdTexture(rect.x, rect.y, rect.w, rect.h, texture, color);
        }

        public static void Box(Rect rect, int color)
        {
            CmdRectangle(rect.x, rect.y, rect.w, rect.h, color);
        }

        public static bool Tile(string text, Texture texture, bool enabled)
        {
            Changed = false;

            State.widgetId++;
            uint id = (State.areaId << 16) | State.widgetId;

            int x = State.widgetX;
            int y = State.widgetY;
            int w = 74;
            int h = 74;

            bool over = enabled && inRect(x, y, w, h, true);
            bool res = buttonLogic(id, over);

            if(texture != null)
                CmdTexture(x, y, w, h, texture, -1);
            else
                CmdRectangle(x, y, w, h, RGBA(196, 196, 196, 96));

            if (isHot(id))
                CmdRectangle(x, y, w, h, RGBA(255, 196, 0, (byte)(isActive(id) ? 196 : 96)));

            var textRect = new Rect(x, y + h, w, TEXT_HEIGHT);
            var p = AlignText(textRect, text, 1, TextAlign.Center);

            CmdText(p.X, p.Y, text, RGBA(255, 255, 255, 100));

            //if (enabled)
            //    CmdText(x + BUTTON_HEIGHT / 2, y + h + BUTTON_HEIGHT / 2 - TEXT_HEIGHT / 2, text, RGBA(255, 255, 255, 200));
            //else
            //    CmdText(x + BUTTON_HEIGHT / 2, y + h + BUTTON_HEIGHT / 2 - TEXT_HEIGHT / 2, text, RGBA(128, 128, 128, 200));

            State.widgetX += w + DEFAULT_SPACING * 5;

            if (State.widgetX + w > Scroll.Right)
            {
                State.widgetX = Scroll.Left;
                State.widgetY += h + DEFAULT_SPACING * 8;
            }

            return res;
        }


        public static bool BigTile(string text, Texture texture, bool enabled)
        {
            Changed = false;

            State.widgetId++;
            uint id = (State.areaId << 16) | State.widgetId;

            int x = State.widgetX;
            int y = State.widgetY;
            int w = 64 + 128;
            int h = 64;

            bool over = enabled && inRect(x, y, w, h, true);
            bool res = buttonLogic(id, over);

            if (isHot(id))
                CmdRectangle(x, y, w, h, RGBA(255, 196, 0, (byte)(isActive(id) ? 196 : 96)));

            if (texture != null)
                CmdTexture(x, y, 64, 64, texture, -1);
            else
                CmdRectangle(x, y, 64, 64, RGBA(196, 196, 196, 96));

            var textRect = new Rect(x + 66, y, w - 66, h);
            var p = AlignText(textRect, text, 1, TextAlign.Left);
            p.Y = y + 2;

            var size = GetTextSize(text, Font);
            CmdText(p.X, p.Y, text, size, RGBA(255, 255, 255, 100));

            //if (enabled)
            //    CmdText(x + BUTTON_HEIGHT / 2, y + h + BUTTON_HEIGHT / 2 - TEXT_HEIGHT / 2, text, RGBA(255, 255, 255, 200));
            //else
            //    CmdText(x + BUTTON_HEIGHT / 2, y + h + BUTTON_HEIGHT / 2 - TEXT_HEIGHT / 2, text, RGBA(128, 128, 128, 200));

            State.widgetX += w + DEFAULT_SPACING * 2;

            if (State.widgetX + w > Scroll.Right)
            {
                State.widgetX = Scroll.Left;
                State.widgetY += h + DEFAULT_SPACING * 2;
            }

            return res;
        }


        public static bool Changed { get; private set; }

        public static bool CheckBox(string text, bool check, bool enabled = true)
        {
            Changed = false;

            State.widgetId++;
            uint id = (State.areaId << 16) | State.widgetId;

            var rect = GetNextRect();

            int x = rect.x; //State.widgetX;
            int y = rect.y; //State.widgetY;
            int w = rect.h; //State.widgetW;
            int h = rect.x; //BUTTON_HEIGHT;

            bool over = enabled && inRect(x, y, w, h, true);
            bool res = buttonLogic(id, over);

            if (res) check = !check;
            if (res) Changed = true;

            CmdRectangle(x, y + BUTTON_HEIGHT / 2 - (CHECK_SIZE + 8) / 2, CHECK_SIZE + 8, CHECK_SIZE + 8, RGBA(128, 128, 128, (byte)(isActive(id) ? 196 : 96)));

            if (check)
            {
                if (enabled)
                    CmdRectangle(x + CHECK_SIZE / 2, y + BUTTON_HEIGHT / 2 - CHECK_SIZE / 2, CHECK_SIZE, CHECK_SIZE, RGBA(255, 255, 255, (byte)(isActive(id) ? 255 : 200)));
                else
                    CmdRectangle(x + CHECK_SIZE / 2, y + BUTTON_HEIGHT / 2 - CHECK_SIZE / 2, CHECK_SIZE, CHECK_SIZE, RGBA(128, 128, 128, 200));
            }

            if (enabled)
                CmdText(x + BUTTON_HEIGHT, y + BUTTON_HEIGHT / 2 - TEXT_HEIGHT / 2, text, isHot(id) ? RGBA(255, 196, 0, 255) : RGBA(255, 255, 255, 200));
            else
                CmdText(x + BUTTON_HEIGHT, y + BUTTON_HEIGHT / 2 - TEXT_HEIGHT / 2, text, RGBA(128, 128, 128, 200));

            return check;
        }

        public static bool Image(Texture texture, int height, bool check, bool enabled)
        {
            Changed = false;

            State.widgetId++;
            uint id = (State.areaId << 16) | State.widgetId;

            int x = State.widgetX;
            int y = State.widgetY;
            int w = State.widgetW;
            int h = height;

            bool over = enabled && inRect(x, y, w, h, true);
            bool res = buttonLogic(id, over);

            if (res) check = !check;
            if (res) Changed = true;

            if (check)
                CmdRectangle(x, y, w, h, RGBA(255, 196, 0, 196));
            else
                CmdRectangle(x, y, w, h, isHot(id) ? RGBA(255, 196, 0, 96) : RGBA(255, 255, 255, 64));

            //CmdRectangle(x, y, w, h, 0, RGBA((byte)(check ? 255 : 0), 0, 0, 255));
            CmdTexture(x + 4, y + 4, w - 8, h - 8, texture, -1);

            State.widgetY += height + DEFAULT_SPACING;

            return check;
        }

        public static bool ImageCheckBox(Rect rect, Texture texture, bool check, bool enabled)
        {
            Changed = false;

            State.widgetId++;
            uint id = (State.areaId << 16) | State.widgetId;

            int x = rect.x;
            int y = rect.y;
            int w = rect.w;
            int h = rect.h;

            bool over = enabled && inRect(x, y, w, h, false);
            bool res = buttonLogic(id, over);

            if (res) check = !check;
            if (res) Changed = true;

            if (check)
                CmdRectangle(x, y, w, h, RGBA(255, 196, 0, 196));
            else
                CmdRectangle(x, y, w, h, isHot(id) ? RGBA(255, 196, 0, 96) : RGBA(255, 255, 255, 64));

            CmdTexture(x + 4, y + 4, w - 8, h - 8, texture, -1);

            return check;
        }

        public static bool ImageButton(Rect rect, Texture texture, bool enabled)
        {
            Changed = false;

            State.widgetId++;
            uint id = (State.areaId << 16) | State.widgetId;

            int x = rect.x;
            int y = rect.y;
            int w = rect.w;
            int h = rect.h;

            bool over = enabled && inRect(x, y, w, h, false);
            bool res = buttonLogic(id, over);
            if (res) Changed = res;

            if (isActive(id))
                CmdRectangle(x, y, w, h, RGBA(255, 196, 0, 196));
            else
                CmdRectangle(x, y, w, h, isHot(id) ? RGBA(255, 196, 0, 96) : RGBA(255, 255, 255, 64));

            CmdTexture(x + 4, y + 4, w - 8, h - 8, texture, -1);

            return res;
        }

        public static Rect GetNextRect()
        {
            int x = State.widgetX + DEFAULT_SPACING;
            int y = State.widgetY + DEFAULT_SPACING;
            int w = horizontal ? BUTTON_WIDTH : State.widgetW - DEFAULT_SPACING * 2;
            int h = horizontal ? State.widgetH - DEFAULT_SPACING * 2 : BUTTON_HEIGHT;

            if (horizontal)
            {
                State.widgetX += BUTTON_WIDTH + DEFAULT_SPACING;
            }
            else
            {
                State.widgetY += BUTTON_HEIGHT + DEFAULT_SPACING;
            }

            return new Rect(x, y, w, h);
        }

        public static Rect GetNextRect(Point textSize)
        {
            int x = State.widgetX + DEFAULT_SPACING;
            int y = State.widgetY + DEFAULT_SPACING;
            int w = horizontal ? BUTTON_WIDTH : State.widgetW - DEFAULT_SPACING * 2;
            int h = horizontal ? State.widgetH - DEFAULT_SPACING * 2 : BUTTON_HEIGHT;

            if (horizontal)
            {
                State.widgetX += BUTTON_WIDTH + DEFAULT_SPACING;
            }
            else
            {
                State.widgetY += BUTTON_HEIGHT + DEFAULT_SPACING;
            }

            var rect = new Rect(x, y, w, h);
            int difference = 0;

            if (textSize.Y > rect.h)
            {
                difference = textSize.Y - rect.h;
                State.widgetY += difference;
                rect.h += difference;
            }

            return rect;
        }

        public static string TextField(string name, string value)
        {
            var rect = GetNextRect();
            var rect2 = new Rect(rect.x + 80, rect.y, rect.w - 80, rect.h);

            Label(rect, name);

            return Textbox(rect2, value, 255, true);
        }

        public static bool ButtonField(string name, string value)
        {
            var rect = GetNextRect();
            var rect2 = new Rect(rect.x + 80, rect.y, rect.w - 80, rect.h);

            Label(rect, name);

            return Button(rect2, value, true);
        }

        public static void Label(string text)
        {
            var rect = GetNextRect();
            Label(rect, text);
        }

        public static void Label(Rect rect, string text)
        {
            Point p = AlignText(rect, text, Font, TextAlign.Left);
            CmdText(p.X, p.Y, text, RGBA(255, 255, 255, 255));
        }

        public static void LeftRight(string left, string right)
        {
            var rect = GetNextRect();

            Point p1 = AlignText(rect, left, Font, TextAlign.Left);
            Point p2 = AlignText(rect, right, Font, TextAlign.Right);

            CmdText(p1.X, p1.Y, left, RGBA(255, 255, 255, 200));
            CmdText(p2.X, p2.Y, right, RGBA(255, 255, 255, 200));
        }

        public static float Slider(string text, float val, float vmin, float vmax, float vinc, bool enabled)
        {
            Changed = false;

            State.widgetId++;
            uint id = (State.areaId << 16) | State.widgetId;

            int x = State.widgetX;
            int y = State.widgetY;
            int w = State.widgetW;
            int h = SLIDER_HEIGHT;

            Point p = AlignText(new Rect(x, y, w, h), text, Font, TextAlign.Left);

            CmdRectangle(x, y, w, h, RGBA(0, 0, 0, 128));

            int range = w - SLIDER_MARKER_WIDTH;

            float u = (val - vmin) / (vmax - vmin);
            if (u < 0) u = 0;
            if (u > 1) u = 1;
            int m = (int)(u * range);

            if (!MouseCaptured && inRect(x, y, w, h, false))
                MouseCaptured = true;

            bool over = inRect(x + m, y, SLIDER_MARKER_WIDTH, SLIDER_HEIGHT, true);
            bool res = buttonLogic(id, over);

            if (isActive(id) && enabled)
            {
                if (State.wentActive)
                {
                    State.dragX = State.mx;
                    State.dragOrig = u;
                }

                if (State.dragX != State.mx)
                {
                    u = State.dragOrig + (float)(State.mx - State.dragX) / (float)range;
                    if (u < 0) u = 0;
                    if (u > 1) u = 1;
                    val = vmin + u * (vmax - vmin);
                    m = (int)(u * range);
                }
            }
            else if (inRect(x, y, w, h, true) && enabled)
            {
                Scroll.Inside = false;

                val += vinc * -State.scroll;
            }

            if (isActive(id) && enabled)
                CmdRectangle(x + m, y, SLIDER_MARKER_WIDTH, SLIDER_HEIGHT, RGBA(255, 196, 0, 196));
            else
                CmdRectangle(x + m, y, SLIDER_MARKER_WIDTH, SLIDER_HEIGHT, isHot(id) ? RGBA(255, 196, 0, 96) : RGBA(255, 255, 255, 64));

            val = (float)Math.Floor(val / vinc) * vinc; // Snap to vinc
            if (val < vmin) val = vmin;
            if (val > vmax) val = vmax;

            string msg = val.ToString();

            Point size = GetTextSize(msg, Font);

            CmdText(x + SLIDER_MARKER_WIDTH + 2, p.Y, text, isHot(id) ? RGBA(255, 196, 0, 255) : RGBA(255, 255, 255, 200));
            CmdText(x + w - size.X, p.Y, msg, isHot(id) ? RGBA(255, 196, 0, 255) : RGBA(255, 255, 255, 200));

            State.widgetY += SLIDER_HEIGHT + DEFAULT_SPACING;

            return val;
        }

        public static void Indent(int size = 8)
        {
            INDENT_SIZE = size;

            State.widgetX += INDENT_SIZE;
            State.widgetW -= INDENT_SIZE;
        }

        public static void Unindent()
        {
            State.widgetX -= INDENT_SIZE;
            State.widgetW += INDENT_SIZE;
        }

        public static void Separator()
        {
            State.widgetY += DEFAULT_SPACING * 3;
        }

        public static int RGBA(byte r, byte g, byte b, byte a)
        {
            return (b) | (g << 8) | (r << 16) | (a << 24);
        }

        public static Point GetTextSize(string text, int font)
        {
            return renderer.GetTextSize(text, font);
        }

        public static Point AlignText(Rect rect, string text, int font, TextAlign align)
        {
            Point size = GetTextSize(text, font);

            if (align == TextAlign.Center)
                return new Point(rect.x + (rect.w - size.X) / 2, rect.y + (rect.h - size.Y) / 2);
            if (align == TextAlign.Left)
                return new Point(rect.x, rect.y + (rect.h - size.Y) / 2);
            if (align == TextAlign.Right)
                return new Point(rect.x + rect.w - size.X, rect.y + (rect.h - size.Y) / 2);

            return new Point(rect.x, rect.y);
        }

        public static Point AlignText(Rect rect, Point size, TextAlign align)
        {
            if (align == TextAlign.Center)
                return new Point(rect.x + (rect.w - size.X) / 2, rect.y + (rect.h - size.Y) / 2);
            if (align == TextAlign.Left)
                return new Point(rect.x, rect.y + (rect.h - size.Y) / 2);
            if (align == TextAlign.Right)
                return new Point(rect.x + rect.w - size.X, rect.y + (rect.h - size.Y) / 2);

            return new Point(rect.x, rect.y);
        }
        #endregion

        #region commands

        private static Rect CurrentScissor;

        static void CmdScissor(int x, int y, int w, int h)
        {
            IMGUICommand cmd = new IMGUICommand();
            cmd.Type = IMGUICommandType.Scissor;
            cmd.Flags = x < 0 ? 0 : 1;	// on/off flag.
            cmd.Color = 0;
            cmd.Rect.x = x;
            cmd.Rect.y = y;
            cmd.Rect.w = w;
            cmd.Rect.h = h;
            Commands.Add(cmd);
            CurrentScissor = cmd.Rect;
        }

        static void CmdRectangle(int x, int y, int w, int h, int color)
        {
            IMGUICommand cmd = new IMGUICommand();
            cmd.Type = IMGUICommandType.Rect;
            cmd.Flags = 0;
            cmd.Color = color;
            cmd.Rect.x = x;
            cmd.Rect.y = y;
            cmd.Rect.w = w;
            cmd.Rect.h = h;
            Commands.Add(cmd);
        }

        static void CmdTexture(int x, int y, int w, int h, Texture texture, int color)
        {
            IMGUICommand cmd = new IMGUICommand();
            cmd.Type = IMGUICommandType.Texture;
            cmd.Flags = 0;
            cmd.Color = color;
            cmd.Texture = texture;
            cmd.Rect.x = x;
            cmd.Rect.y = y;
            cmd.Rect.w = w;
            cmd.Rect.h = h;
            Commands.Add(cmd);
        }

        static void CmdText(int x, int y, string text, int color)
        {
            IMGUICommand cmd = new IMGUICommand();
            cmd.Type = IMGUICommandType.Text;
            cmd.Flags = 0;
            cmd.Color = color;
            cmd.Rect.x = x;
            cmd.Rect.y = y;
            cmd.Rect.w = 100;
            cmd.Rect.h = TEXT_HEIGHT;
            cmd.Text.x = x;
            cmd.Text.y = y;
            cmd.Text.Text = text;
            cmd.Text.Font = Font;
            Commands.Add(cmd);
        }

        static void CmdText(int x, int y, string text, Point size, int color)
        {
            IMGUICommand cmd = new IMGUICommand();
            cmd.Type = IMGUICommandType.Text;
            cmd.Flags = 0;
            cmd.Color = color;
            cmd.Rect.x = x;
            cmd.Rect.y = y;
            cmd.Rect.w = size.X;
            cmd.Rect.h = size.Y;
            cmd.Text.x = x;
            cmd.Text.y = y;
            cmd.Text.Text = text;
            cmd.Text.Font = Font;
            Commands.Add(cmd);
        }

        #endregion

        private static bool IsScissoring;
        private static Rect ScissorRect;

        private static void DrawRoundedRect(int x, int y, int w, int h, int radius, int color)
        {
            List<Vertex2> list = new List<Vertex2>();

            radius += 1;

            Shape2D shape = new Shape2D();

            shape.Vertex(x + radius, y, color);
            shape.Vertex(x + w - radius, y, color);

            for (float t = 3.1415f * 1.5f; t < 3.1415f * 2; t += 0.1f)
            {
                float sx = x + w - radius + (float)Math.Cos(t) * radius;
                float sy = y + radius + (float)Math.Sin(t) * radius;
                shape.Vertex(sx, sy, color);
            }

            shape.Vertex(x + w, y + radius, color);
            shape.Vertex(x + w, y + h - radius, color);

            for (float t = 0; t < 3.1415f * 0.5f; t += 0.1f)
            {
                float sx = x + w - radius + (float)Math.Cos(t) * radius;
                float sy = y + h - radius + (float)Math.Sin(t) * radius;
                shape.Vertex(sx, sy, color);
            }

            shape.Vertex(x + w - radius, y + h, color);
            shape.Vertex(x + radius, y + h, color);

            for (float t = 3.1415f * 0.5f; t < 3.1415f; t += 0.1f)
            {
                float sx = x + radius + (float)Math.Cos(t) * radius;
                float sy = y + h - radius + (float)Math.Sin(t) * radius;
                shape.Vertex(sx, sy, color);
            }

            shape.Vertex(x, y + h - radius, color);
            shape.Vertex(x, y + radius, color);

            for (float t = 3.1415f; t < 3.1415f * 1.5f; t += 0.1f)
            {
                float sx = x + radius + (float)Math.Cos(t) * radius;
                float sy = y + radius + (float)Math.Sin(t) * radius;
                shape.Vertex(sx, sy, color);
            }

           // Game.Screen2D.Draw_Custom(0, CONST_TV_PRIMITIVETYPE.TV_TRIANGLEFAN, shape.Vertices.ToArray(), shape.Vertices.Count);
        }

        private static IMGUIRenderer renderer = new IMGUIRenderer();

        public static void SetWhiteTexture(Texture texture)
        {
            renderer.Spritebatch.SetWhiteTexture(texture);
        }

        public static void Draw()
        {
            renderer.StartBatch();

            foreach (IMGUICommand cmd in IMGUI.Commands)
            {
                if (cmd.Type == IMGUICommandType.Rect)
                {
                    if (IsScissoring && !cmd.Rect.Intersects(ScissorRect))
                        continue;

                    renderer.DrawTexture(cmd.Texture, cmd.Rect.x, cmd.Rect.y, cmd.Rect.w, cmd.Rect.h, cmd.Color);
                }
                else if (cmd.Type == IMGUICommandType.Texture)
                {
                    if (IsScissoring && !cmd.Rect.Intersects(ScissorRect))
                        continue;

                    renderer.DrawTexture(cmd.Texture, cmd.Rect.x, cmd.Rect.y, cmd.Rect.w, cmd.Rect.h, cmd.Color);
                }
                else if (cmd.Type == IMGUICommandType.Text)
                {
                    if (IsScissoring && !cmd.Rect.Intersects(ScissorRect))
                        continue;

                    renderer.DrawText(cmd.Text.Text, cmd.Text.x, cmd.Text.y, cmd.Text.Font, cmd.Color);
                }
                else if (cmd.Type == IMGUICommandType.Scissor)
                {
                    ScissorRect = cmd.Rect;
                    IsScissoring = cmd.Flags != 0;

                    if (!IsScissoring)
                        ScissorRect = new Rect { x = 0, y = 0, w = (int)RenderView.Active.Size.X, h = (int)RenderView.Active.Size.Y };
                    else
                        ScissorRect = new Rect { x = cmd.Rect.x, y = cmd.Rect.y, w = cmd.Rect.w, h = cmd.Rect.h };

                    //Game.Text2D.Action_EndText();
                    renderer.Scissor(ScissorRect.x, ScissorRect.y, ScissorRect.w, ScissorRect.h);
                    //Game.Text2D.Action_BeginText(true);
                }
            }

            renderer.EndBatch(true);
        }

        public static void Clear()
        {
            Commands.Clear();
        }
    }

}
