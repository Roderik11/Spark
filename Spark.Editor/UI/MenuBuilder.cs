using Squid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Editor
{
    public static class MenuBuilder
    {
        public static DropDownButton CreateMenuItem(Frame parent, string name, bool hotDrop = true)
        {
            DropDownButton btn = new DropDownButton();
            btn.Align = Alignment.BottomLeft;
            btn.HotDrop = hotDrop;
            btn.Text = name;
            btn.Size = new Point(70, 26);
            btn.MinSize = new Point(70, 20);
            btn.Dock = DockStyle.Left;
            btn.Style = "menu";
            btn.Margin = new Margin(1, 1, 0, 1);
            btn.AutoSize = AutoSize.Horizontal;
            btn.Dropdown.Size = new Point(280, 300);
            btn.Dropdown.Padding = new Squid.Margin(1);
            btn.Dropdown.AutoSize = AutoSize.Vertical;
            parent.Controls.Add(btn);

            return btn;
        }

        public static void AddSeparator(DropDownButton parent)
        {
            ImageControl frame = new ImageControl
            {
                Size = new Point(1, 1),
                Texture = "whitetex.png",
                Dock = DockStyle.Top,
                Color = ColorInt.ARGB(1f, .25f, .25f, .25f),
                Margin = new Margin(8, 4, 8, 4),
                Tiling = TextureMode.Stretch
            };

            parent.Dropdown.Controls.Add(frame);
        }

        public static DropDownButton AddMenuItem(DropDownButton parent, string name, MouseEvent mouseCLick)
        {
            DropDownButton btn = new DropDownButton();
            btn.HotDrop = true;
            btn.Align = Alignment.TopRight;
            btn.Text = name;
            btn.Size = new Point(100, 26);
            btn.Dock = DockStyle.Top;
            btn.Style = "menuitem";
            btn.Margin = new Margin(0, 0, 0, 1);
            btn.Dropdown.MinSize = new Point();
            btn.Dropdown.Size = new Point(200, 300);
            btn.Dropdown.Padding = new Squid.Margin(1);
            btn.Dropdown.AutoSize = AutoSize.Vertical;
            btn.MouseClick += mouseCLick;

            if (parent.Dropdown != null)
            {
                parent.Dropdown.Controls.Add(btn);
                parent.Dropdown.AutoSize = Squid.AutoSize.Vertical;
            }

            return btn;
        }
    }
}
