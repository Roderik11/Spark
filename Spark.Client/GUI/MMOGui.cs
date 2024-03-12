using System.Collections;
using System.Collections.Generic;
using Squid;
using System;
using System.Reflection;

namespace Spark.Client
{
    public class MMOGui : Desktop
    {
        public Inventory inventory;
        public Skillbook skillbook;
        public ChatWindow chatwindow;
        public CharacterSheet charsheet;
        public ActionBar actionbar1;
        public ActionBar actionbar2;
        public StatusBar statusbar;
        public MiniMap minimap;

        private Frame top;
        private Frame bottom;

        public MMOGui()
        {
            CreateSkin();
            CreateGui();
        }

        void CreateSkin()
        {
            ControlStyle baseStyle = new ControlStyle();
            baseStyle.Tiling = TextureMode.Grid;
            baseStyle.Grid = new Margin(3);
            baseStyle.Texture = "button_default.dds";
            baseStyle.Hot.Texture = "button_hot.dds";
            baseStyle.Default.Texture = "button_default.dds";
            baseStyle.Pressed.Texture = "button_down.dds";
            baseStyle.Focused.Texture = "button_hot.dds";
            baseStyle.SelectedPressed.Texture = "button_down.dds";
            baseStyle.SelectedFocused.Texture = "button_hot.dds";
            baseStyle.Selected.Texture = "button_hot.dds";
            baseStyle.SelectedHot.Texture = "button_hot.dds";
            baseStyle.CheckedPressed.Texture = "button_down.dds";
            baseStyle.CheckedFocused.Texture = "button_down.dds";
            baseStyle.Checked.Texture = "button_down.dds";
            baseStyle.CheckedHot.Texture = "button_down.dds";

            ControlStyle itemStyle = new ControlStyle(baseStyle);
            itemStyle.TextPadding = new Margin(10, 0, 0, 0);
            itemStyle.TextAlign = Alignment.MiddleLeft;

            ControlStyle buttonStyle = new ControlStyle(baseStyle);
            buttonStyle.TextPadding = new Margin(0);
            buttonStyle.TextAlign = Alignment.MiddleCenter;

            ControlStyle tooltipStyle = new ControlStyle(buttonStyle);
            tooltipStyle.TextPadding = new Margin(8);
            tooltipStyle.TextAlign = Alignment.TopLeft;
            tooltipStyle.Texture = "border.dds";
            tooltipStyle.Tiling = TextureMode.Grid;
            tooltipStyle.Grid = new Margin(2);
            tooltipStyle.BackColor = ColorInt.RGBA(0, 0, 0, .9f);

            ControlStyle inputStyle = new ControlStyle();
            inputStyle.Texture = "input_default.dds";
            inputStyle.Hot.Texture = "input_focused.dds";
            inputStyle.Focused.Texture = "input_focused.dds";
            inputStyle.TextPadding = new Margin(8);
            inputStyle.Tiling = TextureMode.Grid;
            inputStyle.Focused.Tint = ColorInt.RGBA(1, 0, 0, 1);
            inputStyle.Grid = new Margin(3);

            ControlStyle windowStyle = new ControlStyle();
            windowStyle.Tiling = TextureMode.Grid;
            windowStyle.Grid = new Margin(8);
            windowStyle.Texture = "window.dds";
            windowStyle.BackColor = ColorInt.RGBA(0, 0, 0, .9f);

            ControlStyle frameStyle = new ControlStyle();
            frameStyle.Tiling = TextureMode.Grid;
            frameStyle.Grid = new Margin(2);
            frameStyle.Texture = "frame.dds";
            frameStyle.TextPadding = new Margin(8);

            ControlStyle vscrollTrackStyle = new ControlStyle();
            vscrollTrackStyle.Tiling = TextureMode.Grid;
            vscrollTrackStyle.Grid = new Margin(3);
            vscrollTrackStyle.Texture = "vscroll_track.dds";

            ControlStyle vscrollButtonStyle = new ControlStyle();
            vscrollButtonStyle.Tiling = TextureMode.Grid;
            vscrollButtonStyle.Grid = new Margin(4);
            vscrollButtonStyle.Texture = "vscroll_button.dds";
            vscrollButtonStyle.Hot.Texture = "vscroll_button_hot.dds";
            vscrollButtonStyle.Pressed.Texture = "vscroll_button_down.dds";

            ControlStyle vscrollUp = new ControlStyle();
            vscrollUp.Default.Texture = "vscrollUp_default.dds";
            vscrollUp.Hot.Texture = "vscrollUp_hot.dds";
            vscrollUp.Pressed.Texture = "vscrollUp_down.dds";
            vscrollUp.Focused.Texture = "vscrollUp_hot.dds";

            ControlStyle hscrollTrackStyle = new ControlStyle();
            hscrollTrackStyle.Tiling = TextureMode.Grid;
            hscrollTrackStyle.Grid = new Margin(3);
            hscrollTrackStyle.Texture = "hscroll_track.dds";

            ControlStyle hscrollButtonStyle = new ControlStyle();
            hscrollButtonStyle.Tiling = TextureMode.Grid;
            hscrollButtonStyle.Grid = new Margin(3);
            hscrollButtonStyle.Texture = "hscroll_button.dds";
            hscrollButtonStyle.Hot.Texture = "hscroll_button_hot.dds";
            hscrollButtonStyle.Pressed.Texture = "hscroll_button_down.dds";

            ControlStyle hscrollUp = new ControlStyle();
            hscrollUp.Default.Texture = "hscrollUp_default.dds";
            hscrollUp.Hot.Texture = "hscrollUp_hot.dds";
            hscrollUp.Pressed.Texture = "hscrollUp_down.dds";
            hscrollUp.Focused.Texture = "hscrollUp_hot.dds";

            ControlStyle checkButtonStyle = new ControlStyle();
            checkButtonStyle.Default.Texture = "checkbox_default.dds";
            checkButtonStyle.Hot.Texture = "checkbox_hot.dds";
            checkButtonStyle.Pressed.Texture = "checkbox_down.dds";
            checkButtonStyle.Checked.Texture = "checkbox_checked.dds";
            checkButtonStyle.CheckedFocused.Texture = "checkbox_checked_hot.dds";
            checkButtonStyle.CheckedHot.Texture = "checkbox_checked_hot.dds";
            checkButtonStyle.CheckedPressed.Texture = "checkbox_down.dds";

            ControlStyle comboLabelStyle = new ControlStyle();
            comboLabelStyle.TextPadding = new Margin(10, 0, 0, 0);
            comboLabelStyle.Default.Texture = "combo_default.dds";
            comboLabelStyle.Hot.Texture = "combo_hot.dds";
            comboLabelStyle.Pressed.Texture = "combo_down.dds";
            comboLabelStyle.Focused.Texture = "combo_hot.dds";
            comboLabelStyle.Tiling = TextureMode.Grid;
            comboLabelStyle.Grid = new Margin(3, 0, 0, 0);

            ControlStyle comboButtonStyle = new ControlStyle();
            comboButtonStyle.Default.Texture = "combo_button_default.dds";
            comboButtonStyle.Hot.Texture = "combo_button_hot.dds";
            comboButtonStyle.Pressed.Texture = "combo_button_down.dds";
            comboButtonStyle.Focused.Texture = "combo_button_hot.dds";

            ControlStyle borderStyle = new ControlStyle();
            borderStyle.Hot.Texture = "border.dds";
            borderStyle.Pressed.Texture = "border.dds";
            borderStyle.Tiling = TextureMode.Grid;
            borderStyle.Grid = new Margin(4);

            ControlStyle labelStyle = new ControlStyle();
            labelStyle.TextAlign = Alignment.TopLeft;
            labelStyle.TextPadding = new Margin(8);

            ControlStyle handleNW = new ControlStyle();
            handleNW.Texture = "handleNW.dds";

            ControlStyle handleNE = new ControlStyle();
            handleNE.Texture = "handleNE.dds";

            labelStyle.TextPadding = new Margin(8);

            Skin skin = new Squid.Skin();

            skin.Add("item", itemStyle);
            skin.Add("textbox", inputStyle);
            skin.Add("button", buttonStyle);
            skin.Add("window", windowStyle);
            skin.Add("frame", frameStyle);
            skin.Add("checkBox", checkButtonStyle);
            skin.Add("comboLabel", comboLabelStyle);
            skin.Add("comboButton", comboButtonStyle);
            skin.Add("vscrollTrack", vscrollTrackStyle);
            skin.Add("vscrollButton", vscrollButtonStyle);
            skin.Add("vscrollUp", vscrollUp);
            skin.Add("hscrollTrack", hscrollTrackStyle);
            skin.Add("hscrollButton", hscrollButtonStyle);
            skin.Add("hscrollUp", hscrollUp);
            skin.Add("multiline", labelStyle);
            skin.Add("tooltip", tooltipStyle);
            skin.Add("border", borderStyle);
            skin.Add("handleNE", handleNE);
            skin.Add("handleNW", handleNW);

            #region cursors

            Point cursorSize = new Point(32, 32);
            Point halfSize = cursorSize / 2;

            CursorSet.Add(Cursors.Default, new Cursor { Texture = "cursors\\Arrow.png", Size = cursorSize, HotSpot = Point.Zero });
            CursorSet.Add(Cursors.Link, new Cursor { Texture = "cursors\\Link.png", Size = cursorSize, HotSpot = Point.Zero });
            CursorSet.Add(Cursors.Move, new Cursor { Texture = "cursors\\Move.png", Size = cursorSize, HotSpot = halfSize });
            CursorSet.Add(Cursors.Select, new Cursor { Texture = "cursors\\Select.png", Size = cursorSize, HotSpot = halfSize });
            CursorSet.Add(Cursors.SizeNS, new Cursor { Texture = "cursors\\SizeNS.png", Size = cursorSize, HotSpot = halfSize });
            CursorSet.Add(Cursors.SizeWE, new Cursor { Texture = "cursors\\SizeWE.png", Size = cursorSize, HotSpot = halfSize });
            CursorSet.Add(Cursors.HSplit, new Cursor { Texture = "cursors\\SizeNS.png", Size = cursorSize, HotSpot = halfSize });
            CursorSet.Add(Cursors.VSplit, new Cursor { Texture = "cursors\\SizeWE.png", Size = cursorSize, HotSpot = halfSize });
            CursorSet.Add(Cursors.SizeNESW, new Cursor { Texture = "cursors\\SizeNESW.png", Size = cursorSize, HotSpot = halfSize });
            CursorSet.Add(Cursors.SizeNWSE, new Cursor { Texture = "cursors\\SizeNWSE.png", Size = cursorSize, HotSpot = halfSize });

            #endregion

            Skin = skin;
        }

        void CreateGui()
        {
            TooltipControl = new SimpleTooltip();

            top = new Frame();
            top.Size = new Point(24, 24);
            top.Dock = DockStyle.Top;
            top.Style = "button";
            Desktop.Controls.Add(top);

            bottom = new Frame();
            bottom.Size = new Point(14, 14);
            bottom.Dock = DockStyle.Bottom;
            bottom.Style = "button";
            Desktop.Controls.Add(bottom);

            Frame center = new Frame();
            center.Dock = DockStyle.Fill;
            Desktop.Controls.Add(center);

            actionbar1 = new ActionBar();
            actionbar2 = new ActionBar();
            statusbar = new StatusBar();
            minimap = new MiniMap();
            inventory = new Inventory();
            skillbook = new Skillbook();
            chatwindow = new ChatWindow();
            charsheet = new CharacterSheet();

            center.Controls.Add(actionbar1);
            center.Controls.Add(actionbar2);
            center.Controls.Add(statusbar);

            chatwindow.Show(Desktop);
            minimap.Show(Desktop);
            inventory.Show(Desktop);
            charsheet.Show(Desktop);
            skillbook.Show(Desktop);
        }
    }

    public class StatusBar : Control
    {
        private Frame frame;

        public StatusBar()
        {
            Size = new Point(44, 20);
            Dock = DockStyle.Bottom;

            frame = new Frame();
            frame.Dock = DockStyle.Center;
            frame.AutoSize = Squid.AutoSize.Horizontal;
            Elements.Add(frame);

            AddBar();
            AddBar();
            AddBar();
        }

        private void AddBar()
        {
            Frame bar1 = new Frame();
            bar1.Size = new Point(110, 10);
            bar1.Style = "window";
            bar1.Dock = DockStyle.Left;
            bar1.Margin = new Squid.Margin(1);
            frame.Controls.Add(bar1);
        }

        void button_DragDrop(Control sender, DragDropEventArgs e)
        {
            ActionButton slot = sender as ActionButton;

            slot.Item = e.DraggedControl.Tag as GuiItem;
        }
    }

    public class Character
    {
        public List<Item> Items = new List<Item>();
        public List<Skill> Skills = new List<Skill>();
        public CharacterItemSlots ItemSlots = new CharacterItemSlots();

        public Character()
        {
            foreach (ItemSlotType type in Enum.GetValues(typeof(ItemSlotType)))
            {
                if (type != ItemSlotType.None)
                    ItemSlots.Add(type, new ItemSlot { Type = type });
            }
        }

        public bool Equip(Item item)
        {
            if (item != null)
            {
                if (item.Equipped) return false;
                if (!ItemSlots.ContainsKey(item.SlotType)) return false;
            }

            ItemSlot slot = ItemSlots[item.SlotType];
            slot.Item = item;

            return true;
        }

        public bool Equip(Item item, ItemSlot slot)
        {
            if (slot == null) return false;

            if (item != null)
            {
                if (item.SlotType != slot.Type) return false;
            }

            slot.Item = item;

            return true;
        }
    }

    public class CharacterItemSlots : Dictionary<ItemSlotType, ItemSlot>
    {

    }

    public class ItemSlot
    {
        private Item _item;

        public ItemSlotType Type { get; set; }

        public Item Item
        {
            get { return _item; }

            set
            {
                if (value != null)
                {
                    if (value.SlotType != Type) return;
                }

                if (_item != null)
                    _item.Equipped = false;

                _item = value;

                if (_item != null)
                    _item.Equipped = true;
            }
        }
    }

    public enum ItemSlotType
    {
        None,
        Head,
        Shoulders,
        Back,
        Chest,
        Waist,
        Legs,
        Feet,
        Arms,
        Hands,
        Neck,
        Finger,
        MainHand,
        OffHand
    }

    public static class GameLogic
    {
        public static Character Player;

        static GameLogic()
        {
            Player = new Character();

            System.Random rnd = new System.Random();
            List<Icon> icons = new List<Icon>();

            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    Icon icon = new Icon();
                    icon.Texture = "weapon-icons.png";
                    icon.Rect = new Rectangle(2 + x * 60, 2 + y * 60, 56, 56);
                    icons.Add(icon);
                }

            }

            for (int i = 0; i < 32; i++)
            {
                int slot = rnd.Next(14);

                string tooltip = "" +
                     "[color=ffffff00]" + "Scorching Waraxe of Absorbtion" + "[/color]" + Environment.NewLine +
                     ((ItemSlotType)slot).ToString() + Environment.NewLine +
                     "\r\n" +
                     "Strength: " + "[color=ff00ff00]" + rnd.Next(60) + "[/color]" + Environment.NewLine +
                     "Vitality: " + "[color=ff00ff00]" + rnd.Next(60) + "[/color]" + Environment.NewLine +
                     "Wisdom: " + "[color=ff00ff00]" + rnd.Next(60) + "[/color]" + Environment.NewLine +
                     "Critical Hit: " + "[color=ff00ff00]" + rnd.Next(60) + "[/color]" + Environment.NewLine +
                     "\r\n" +
                     "[color=ff0066ff]" + "4% Life Steal" + "[/color]" + Environment.NewLine +
                     "[color=ff0066ff]" + "25% Chance to fart on hit" + "[/color]" + Environment.NewLine +
                     "[color=ff0066ff]" + "11% Chance to cast Fireburst on attack" + "[/color]" + Environment.NewLine +
                     "[color=ff0066ff]" + "Cannot be used when drunk" + "[/color]";

                Player.Items.Add(new Item
                {
                    Name = "item",
                    SlotType = (ItemSlotType)slot,
                    Icon = icons[rnd.Next(icons.Count)],
                    Count = rnd.Next(50),
                    Tooltip = tooltip
                });
            }

            for (int i = 0; i < 32; i++)
            {
                string tooltip = "Name" + Environment.NewLine +
                    "Cost: " + "[color=ffff00ff]" + rnd.Next(100) + "[/color]" + Environment.NewLine +
                    "Cooldown: " + "[color=ff00ff00]" + rnd.Next(60) + "[/color] sec" + Environment.NewLine +
                    "\nHateful Saint's Abjuration of the Vorticies of Secrets\n" +
                    "Blind Spectre's Communion of the Ancient Edge";

                Player.Skills.Add(new Skill
                {
                    Name = "skill",
                    Icon = icons[rnd.Next(icons.Count)],
                    Tooltip = tooltip
                });
            }
        }
    }

    public class GuiItem
    {
        public string Tooltip;
        public string Name;
        public Icon Icon;
        public int Count;
    }

    public class Icon
    {
        public string Name;
        public string Texture;
        public Rectangle Rect;
    }

    public class Skill : GuiItem
    {
    }

    public class Item : GuiItem
    {
        public bool Equipped;
        public bool Stackable;
        public ItemSlotType SlotType;
    }

    public class ActionButton : Control
    {
        private GuiItem _item;
        private ImageControl image;
        private Button button;
        private Label counter;

        public GuiItem Item;

        public ActionButton()
        {
            image = new ImageControl();
            image.Dock = DockStyle.Fill;
            image.NoEvents = true;
            image.Texture = "border.dds";
            image.TextureRect = new Rectangle(0, 0, 64, 64);
            Elements.Add(image);

            button = new Button();
            button.Dock = DockStyle.Fill;
            button.Style = "border";
            button.NoEvents = true;
            Elements.Add(button);

            counter = new Label();
            counter.Dock = DockStyle.Top;
            counter.Size = new Point(16, 16);
            counter.TextAlign = Alignment.MiddleRight;
            counter.NoEvents = true;
            Elements.Add(counter);
        }

        public ActionButton(GuiItem item)
            : this()
        {
            Item = item;
        }

        protected override void OnStateChanged()
        {
            base.OnStateChanged();
            button.State = State;
        }

        protected override void OnUpdate()
        {
            if (Item != null)
            {
                Tooltip = Item.Tooltip;
                image.Texture = Item.Icon.Texture;
                image.TextureRect = Item.Icon.Rect;
                counter.Visible = Item.Count > 0;
                counter.Text = Item.Count.ToString();
            }

            base.OnUpdate();
        }

        protected override void DrawStyle(Style style, float opacity) { }
        protected override void DrawText(Style style, float opacity) { }
    }

    public class Inventory : GuiWindow
    {
        private TextBox search;
        private ScrollView view;
        private FlowLayoutFrame flow;
        private Frame left;
        private Frame top;
        private Frame bottom;

        public Inventory()
        {
            Resizable = true;
            Size = new Point(400, 300);
            Position = new Point(400, 200);
            Titlebar.Text = "Inventory";

            left = new Frame();
            left.Size = new Point(50, 50);
            left.Dock = DockStyle.Left;
            left.Style = "window";
            Controls.Add(left);

            for (int i = 0; i < 6; i++)
            {
                ActionButton slot = new ActionButton();
                slot.Size = new Point(40, 40);
                slot.Dock = DockStyle.Top;
                slot.Margin = new Squid.Margin(5, 5, 5, 0);
                left.Controls.Add(slot);
            }

            top = new Frame();
            top.Size = new Point(58, 58);
            top.Dock = DockStyle.Top;
            Controls.Add(top);

            search = new TextBox();
            search.Size = new Point(200, 28);
            search.Dock = DockStyle.Bottom;
            search.Style = "textbox";
            search.Text = "Search...";
            search.Margin = new Squid.Margin(8, 0, 8, 0);
            search.GotFocus += delegate(Control sender) { search.Text = ""; };
            search.LostFocus += delegate(Control sender) { search.Text = "Search..."; };
            top.Controls.Add(search);

            bottom = new Frame();
            bottom.Size = new Point(38, 38);
            bottom.Dock = DockStyle.Bottom;
            bottom.Style = "border";
            bottom.Margin = new Squid.Margin(-1, 0, 0, 0);
            Controls.Add(bottom);

            view = new ScrollView();
            view.Dock = DockStyle.Fill;
            view.AllowDrop = true;
            Controls.Add(view);

            flow = new FlowLayoutFrame();
            flow.FlowDirection = FlowDirection.LeftToRight;
            flow.HSpacing = flow.VSpacing = 4;
            flow.AutoSize = Squid.AutoSize.Vertical;
            flow.Dock = DockStyle.Top;
            view.Controls.Add(flow);

            Refresh();
        }

        void Refresh()
        {
            flow.Controls.Clear();

            foreach (Item item in GameLogic.Player.Items)
            {
                if (item.Equipped) continue;

                ActionButton button = new ActionButton(item);
                button.Size = new Point(40, 40);
                button.Style = "button";
                button.MouseDrag += button_MouseDrag;

                flow.Controls.Add(button);
            }
        }

        void button_MouseDrag(Control sender, MouseEventArgs args)
        {
            ActionButton slot = sender as ActionButton;

            ImageControl image = new ImageControl();
            image.Texture = slot.Item.Icon.Texture;
            image.TextureRect = slot.Item.Icon.Rect;
            image.Size = new Point(40, 40);
            image.Position = Gui.MousePosition;
            image.Tag = slot.Item;

            DoDragDrop(image);
        }
    }

    public class ScrollView : Control
    {
        public Panel Panel;
        public Frame Content;

        public ControlCollection Controls { get { return Content.Controls; } }

        public ScrollView()
        {
            Dock = DockStyle.Fill;
            Margin = new Squid.Margin(2);

            Panel = new Panel();
            Panel.Dock = DockStyle.Fill;
            Panel.VScroll.ButtonUp.Visible = false;
            Panel.VScroll.ButtonDown.Visible = false;
            Panel.VScroll.Size = new Point(13, 12);
            Panel.VScroll.Slider.Style = "window";
            Panel.VScroll.Slider.Button.Style = "button";
            Panel.VScroll.Dock = DockStyle.Right;
            Panel.VScroll.Margin = new Margin(4, 0, 0, 0);
            Panel.HScroll.Size = new Point(0, 0);
            Elements.Add(Panel);

            Content = new Frame();
            Content.AutoSize = AutoSize.Vertical;
            Content.Dock = DockStyle.Top;
            Panel.Content.Controls.Add(Content);
        }

        public void Scroll(int value)
        {
            Panel.VScroll.Value = value;
        }

        protected override void OnUpdate()
        {
            if (Gui.MouseScroll != 0 && Desktop.HotControl != null)
            {
                if (Hit(Gui.MousePosition.x, Gui.MousePosition.y))
                {
                    if (Desktop.HotControl == this || Desktop.HotControl.IsChildOf(this))
                    {
                        Panel.VScroll.MouseScrollSpeed = 64f / (float)(Panel.Content.Size.y - Panel.ClipFrame.Size.y);
                        Panel.VScroll.Scroll(Gui.MouseScroll);
                    }
                }
            }

            base.OnUpdate();
        }
    }

    public class Skillbook : GuiWindow
    {
        private TextBox searchfield;
        private ScrollView view;
        private FlowLayoutFrame flow;

        public Skillbook()
        {
            Resizable = true;
            Size = new Point(400, 300);
            Position = new Point(400, 200);
            Titlebar.Text = "Skills";

            view = new ScrollView();
            view.Dock = DockStyle.Fill;
            Controls.Add(view);

            flow = new FlowLayoutFrame();
            flow.FlowDirection = FlowDirection.LeftToRight;
            flow.HSpacing = flow.VSpacing = 4;
            flow.AutoSize = Squid.AutoSize.Vertical;
            flow.Dock = DockStyle.Top;
            view.Controls.Add(flow);

            Refresh();
        }

        void Refresh()
        {
            flow.Controls.Clear();

            foreach (Skill item in GameLogic.Player.Skills)
            {
                ActionButton button = new ActionButton(item);
                button.Size = new Point(40, 40);
                button.Style = "button";
                button.MouseDrag += button_MouseDrag;

                flow.Controls.Add(button);
            }
        }

        void button_MouseDrag(Control sender, MouseEventArgs args)
        {
            ActionButton slot = sender as ActionButton;

            ImageControl image = new ImageControl();
            image.Texture = slot.Item.Icon.Texture;
            image.TextureRect = slot.Item.Icon.Rect;
            image.Size = new Point(40, 40);
            image.Position = sender.Location;
            image.Tag = slot.Item;

            DoDragDrop(image);
        }
    }

    public class CharacterSheet : GuiWindow
    {
        private ScrollView stats;
        private SplitContainer split;
        private Frame paperdoll;

        private Frame top;
        private Frame left;
        private Frame right;
        private Frame bottom;

        public CharacterSheet()
        {
            Resizable = true;
            Size = new Point(400, 300);
            Position = new Point(100, 100);
            Titlebar.Text = "Character";

            split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            Controls.Add(split);

            stats = new ScrollView();
            stats.Dock = DockStyle.Fill;
            split.SplitFrame1.Controls.Add(stats);

            paperdoll = new Frame();
            paperdoll.Dock = DockStyle.Fill;
            split.SplitFrame2.Controls.Add(paperdoll);

            ImageControl image = new ImageControl { Texture = "paperdoll.jpg" };
            image.Dock = DockStyle.Fill;
            paperdoll.Controls.Add(image);

            //top = new Frame { Size = new Point(44, 44), Dock = DockStyle.Top }; paperdoll.Controls.Add(top);
            //bottom = new Frame { Size = new Point(44, 44), Dock = DockStyle.Bottom }; paperdoll.Controls.Add(bottom);
            left = new Frame { Size = new Point(44, 44), Dock = DockStyle.Left }; paperdoll.Controls.Add(left);
            right = new Frame { Size = new Point(44, 44), Dock = DockStyle.Right }; paperdoll.Controls.Add(right);

            int i = 0;
            foreach (KeyValuePair<ItemSlotType, ItemSlot> pair in GameLogic.Player.ItemSlots)
            {
                ActionButton button = new ActionButton();
                button.Size = new Point(40, 40);
                button.Style = "button";
                button.Dock = DockStyle.Top;
                button.Margin = new Squid.Margin(2);
                button.AllowDrop = true;
                button.DragDrop += button_DragDrop;
                button.Tooltip = pair.Key.ToString();
                button.Tag = pair.Value;

                if (i < 6)
                    left.Controls.Add(button);
                else
                    right.Controls.Add(button);

                i++;
            }
        }

        public void Hightlight(ItemSlotType type)
        {

        }

        void button_DragDrop(Control sender, DragDropEventArgs e)
        {
            ActionButton slot = sender as ActionButton;

            if (e.DraggedControl.Tag is Item)
            {
                Item item = e.DraggedControl.Tag as Item;
                ItemSlot itemSlot = slot.Tag as ItemSlot;

                if (GameLogic.Player.Equip(item, itemSlot))
                    slot.Item = item;
            }
        }
    }

    public class ActionBar : Frame
    {
        public ActionBar()
        {
            Size = new Point(44, 44);
            Dock = DockStyle.Bottom;

            Frame frame = new Frame();
            frame.Dock = DockStyle.CenterX;
            frame.AutoSize = Squid.AutoSize.Horizontal;
            Controls.Add(frame);

            for (int i = 0; i < 8; i++)
            {
                ActionButton button = new ActionButton();
                button.Size = new Point(40, 40);
                button.Style = "button";
                button.Dock = DockStyle.Left;
                button.Margin = new Squid.Margin(2);
                button.AllowDrop = true;
                button.DragDrop += button_DragDrop;
                frame.Controls.Add(button);
            }
        }

        void button_DragDrop(Control sender, DragDropEventArgs e)
        {
            ActionButton slot = sender as ActionButton;

            slot.Item = e.DraggedControl.Tag as GuiItem;
        }
    }


    public class MiniMap : Window
    {
        private Frame map;
        private Frame toolbar;
        private Button handle;

        private Point clickedPos;
        private Point oldSize;

        public MiniMap()
        {
            Style = "window";
            Size = new Point(256, 256);
            MaxSize = new Point(512, 512);
            MinSize = new Point(128, 128);
            Resizable = false;

            handle = new Button { Size = new Point(16, 16), Style = "handleNW" };
            handle.Cursor = Cursors.SizeNWSE;
            handle.MouseDown += handle_OnMouseDown;
            handle.MousePress += handle_OnMousePress;
            Elements.Add(handle);

            toolbar = new Frame { Size = new Point(28, 28) };
            toolbar.Dock = DockStyle.Right;
            toolbar.Style = "frame";
            Elements.Add(toolbar);

            for (int i = 0; i < 5; i++)
            {
                Button button = new Button { Size = new Point(24, 26) };
                button.Dock = DockStyle.Top;
                button.Margin = new Squid.Margin(2, 2, 2, 0);
                toolbar.Controls.Add(button);
            }

            map = new Frame();
            map.Dock = DockStyle.Fill;
            map.Scissor = true;
            map.Margin = new Squid.Margin(1);
            Controls.Add(map);

            ImageControl image = new ImageControl();
            image.Texture = "minimap.jpg";
            image.Size = new Point(1024, 1024);
            image.MousePress += image_MousePress;
            map.Controls.Add(image);

        }

        void image_MousePress(Control sender, MouseEventArgs args)
        {
            sender.Position += Gui.MouseMovement;
        }

        void handle_OnMouseDown(Control sender, MouseEventArgs args)
        {
            if (args.Button > 0) return;

            clickedPos = Gui.MousePosition;
            oldSize = Size;
        }

        void handle_OnMousePress(Control sender, MouseEventArgs args)
        {
            if (args.Button > 0) return;

            ResizeTo(oldSize + (clickedPos - Gui.MousePosition), AnchorStyles.Top | AnchorStyles.Left);
        }

        protected override void OnUpdate()
        {
            Position = Parent.Size - Size - new Point(0, 14);

            base.OnUpdate();
        }
    }

    public class ChatTab : Control
    {
        public Label Output { get; private set; }
        public ScrollBar Scrollbar { get; private set; }
        public Frame Frame { get; private set; }

        public ChatTab()
        {
            Size = new Point(100, 100);
            Dock = DockStyle.Fill;

            Scrollbar = new ScrollBar();
            Scrollbar.Dock = DockStyle.Left;
            Scrollbar.Size = new Point(25, 25);
            Scrollbar.Margin = new Margin(0, 8, 8, 8);
            Scrollbar.Size = new Squid.Point(14, 10);
            Scrollbar.Slider.Style = "vscrollTrack";
            Scrollbar.Slider.Button.Style = "vscrollButton";
            Scrollbar.ButtonUp.Style = "vscrollUp";
            Scrollbar.ButtonUp.Size = new Squid.Point(10, 20);
            Scrollbar.ButtonDown.Style = "vscrollUp";
            Scrollbar.ButtonDown.Size = new Squid.Point(10, 20);
            Scrollbar.Slider.Margin = new Margin(0, 2, 0, 2);
            Scrollbar.ShowAlways = true;
            Elements.Add(Scrollbar);

            Frame = new Frame();
            Frame.Dock = DockStyle.Fill;
            Frame.Scissor = true;
            Elements.Add(Frame);

            Output = new Label();
            Output.BBCodeEnabled = true;
            Output.TextWrap = true;
            Output.AutoSize = Squid.AutoSize.Vertical;
            Output.Text = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.";
            Output.Style = "multiline";
            Output.Margin = new Margin(8, 8, 8, 8);
            Output.TextAlign = Alignment.BottomLeft;
            Frame.Controls.Add(Output);
        }

        protected override void OnUpdate()
        {
            // force the width to be that of its parent
            Output.Size = new Point(Frame.Size.x, Output.Size.y);

            // move the label up/down using the scrollbar value
            if (Output.Size.y < Frame.Size.y) // no need to scroll
            {
                Scrollbar.Visible = false; // hide scrollbar
                Output.Position = new Point(0, Frame.Size.y - Output.Size.y); // set fixed position
            }
            else
            {
                Scrollbar.Scale = Math.Min(1, (float)Frame.Size.y / (float)Output.Size.y);
                Scrollbar.Visible = true; // show scrollbar
                Output.Position = new Point(0, (int)((Frame.Size.y - Output.Size.y) * Scrollbar.EasedValue));
            }

            // the mouse is scrolling and there is any control hovered
            if (Gui.MouseScroll != 0 && Desktop.HotControl != null)
            {
                // ok, lets check if the mouse is anywhere near us
                if (Hit(Gui.MousePosition.x, Gui.MousePosition.y))
                {
                    // now lets check if its really this window or anything in it
                    if (Desktop.HotControl == this || Desktop.HotControl.IsChildOf(this))
                        Scrollbar.Scroll(Gui.MouseScroll);
                }
            }
        }

        public void Append(string text)
        {
            // check for null/empty
            if (string.IsNullOrEmpty(text))
                return;

            // return if only whitespaces were entered
            if (text.Trim().Length == 0)
                return;

            string prefix = ""; // "[Username]: ";

            if (string.IsNullOrEmpty(Output.Text))
                Output.Text = prefix + text;
            else
                Output.Text += Environment.NewLine + prefix + text;

            Scrollbar.Value = 1; // scroll down
        }
    }

    public class ChatWindow : Window
    {
        public TextBox input;

        private Point clickedPos;
        private Point oldSize;
        private Button handle;
        private ChatTab chat;
        private TabControl tabs;

        public ChatWindow()
        {
            Style = "window";
            Resizable = false;
            Size = new Point(384, 256);
            MinSize = new Point(192, 128);
            MaxSize = new Point(786, 512);

            handle = new Button { Size = new Point(16, 16), Position = new Point(384 - 16, 0), Style = "handleNE" };
            handle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            handle.Cursor = Cursors.SizeNESW;
            handle.MouseDown += handle_OnMouseDown;
            handle.MousePress += handle_OnMousePress;
            Elements.Add(handle);

            input = new TextBox();
            input.Size = new Point(100, 35);
            input.Dock = DockStyle.Bottom;
            input.TextCommit += Input_OnTextCommit;
            input.Style = "textbox";
            input.Margin = new Squid.Margin(2);
            Controls.Add(input);

            tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            tabs.ButtonFrame.Size = new Point(28, 28);
            Controls.Add(tabs);

            AddPage("Global");
            AddPage("Party");
            AddPage("System");
        }

        void handle_OnMouseDown(Control sender, MouseEventArgs args)
        {
            if (args.Button > 0) return;

            clickedPos = Gui.MousePosition;
            oldSize = Size;
        }

        void handle_OnMousePress(Control sender, MouseEventArgs args)
        {
            if (args.Button > 0) return;

            Point p = Gui.MousePosition - clickedPos;
            p.y = clickedPos.y - Gui.MousePosition.y;

            ResizeTo(oldSize + p, AnchorStyles.Top | AnchorStyles.Right);
        }

        void AddPage(string name)
        {
            TabPage page = new TabPage();
            page.Button.Text = name;
            page.Button.Size = new Point(100, 10);

            Button close = new Button();
            close.Size = new Point(16, 16);
            close.Dock = DockStyle.Right;
            close.Margin = new Squid.Margin(6);
            page.Button.Controls.Add(close);

            Button config = new Button();
            config.Size = new Point(16, 16);
            config.Dock = DockStyle.Left;
            config.Margin = new Squid.Margin(6);
            page.Button.Controls.Add(config);

            page.Button.Update += delegate(Control sender)
            {
                config.Visible = page.Button.Checked;
                close.Visible = page.Button.Checked || page.Button.Hit(Gui.MousePosition.x, Gui.MousePosition.y);
            };

            page.Button.Controls.Add(close);
            chat = new ChatTab();
            page.Controls.Add(chat);

            tabs.TabPages.Add(page);
        }

        void Input_OnTextCommit(object sender, EventArgs e)
        {
            // Append(Input.Text); // try to append the text
            input.Text = string.Empty;
            input.Focus();
        }

        protected override void OnUpdate()
        {
            Position = new Point(0, Parent.Size.y - Size.y - 14);

            base.OnUpdate();
        }
    }
}