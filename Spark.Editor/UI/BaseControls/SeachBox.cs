using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squid;
using Spark;
using System.Reflection;
using System.IO;

namespace Spark.Editor
{
    public class SearchBox : TextBox
    {
        public ImageControl Icon;
        public ImageControl Button;

        public SearchBox()
        {
            Style = "searchbox";

            Icon = new ImageControl
            {
                Size = new Point(16, 16),
                Dock = DockStyle.Left,
                Texture = "icon_search.dds",
                Tiling = TextureMode.Center,
                Color = ColorInt.ARGB(1f, .5f, .5f, .5f)
            };

            Button = new ImageControl
            {
                Size = new Point(16, 16),
                Style = "tab",
                Dock = DockStyle.Right,
                Texture = "minicross.dds",
                Tiling = TextureMode.Center,
                Color = ColorInt.ARGB(1f, .7f, .7f, .7f),
                Margin = new Margin(2),
                Visible = false
            };

            Elements.Add(Icon);
            Elements.Add(Button);

            Button.MouseClick += Button_MouseClick;
            this.TextChanged += SearchBox_TextChanged;
        }

        private void Button_MouseClick(Control sender, MouseEventArgs args)
        {
            Text = string.Empty;
        }

        private void SearchBox_TextChanged(Control sender)
        {
            bool isempty = string.IsNullOrEmpty(Text);
            Button.Visible = !isempty;
        }
    }
}
