using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squid;
using Spark;
using System.Reflection;
using System.ComponentModel;
using SharpDX;

using Point = Squid.Point;
using SharpDX.Direct3D11;
using System.Collections;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Menu;


namespace Spark.Client
{
    [PropertyControl(typeof(IAsset))]
    public class AssetProperty : PropertyControl
    {
        public Button Button { get; private set; }
        private readonly ImageControl thumbnail;

        public AssetProperty(GUIProperty property) : base(property)
        {
            RowHeight = 68;

            var asset = property.GetValue() as IAsset;

            var btn = new Button
            {
                Size = new Point(24, 16),
                Dock = DockStyle.Right,
                Style = "textbox",
            };

            Button = new Button
            {
                Size = new Point(26, 20),
                Dock = DockStyle.Fill,
                Style = "textbox",
                Text = asset != null ? asset.Name : "- None -"
            };

            btn.MouseClick += Button_MouseClick;
            Button.MouseDoubleClick += (s, e) =>
            {
                if (property.GetValue() is IAsset value)
                {
                    if(string.IsNullOrEmpty(value.Path))
                        Selector.SelectedObject = value;
                    else
                        Selector.SelectedObject = AssetDatabase.GetAssetReader(value.Path);
                }
            };

            Frame frame = new Frame { Dock = DockStyle.Top, Size = new Point(20, 26) };
            frame.Controls.Add(btn);
            frame.Controls.Add(Button);

            var shadow = new Frame
            {
                Size = new Point(60, 60),
                Style = "dropshadow",
                Dock = DockStyle.Left,
                Margin = new Margin(0,0,6,0),
                Padding = new Margin(0, 0, 1, 1)
            };
            Controls.Add(shadow);

            shadow.GetElements().Add(new Frame
            {
                Style = "border",
                Dock = DockStyle.Fill,
                Margin = new Margin(0, 0, 1, 1)
            });

            thumbnail = new ImageControl
            {
                Size = new Point(62, 62),
                Dock = DockStyle.Fill,
                Texture = AssetDatabase.GetThumbnail(asset)
            };
            shadow.Controls.Add(thumbnail);

            ImageControl icon = new ImageControl
            {
                Dock = DockStyle.Fill,
                NoEvents = true,
                Texture = "icon_more.png",
                Tiling = TextureMode.Center,

            };

            btn.GetElements().Add(icon);

            thumbnail.AllowDrop = true;
            thumbnail.DragResponse += Thumbnail_DragResponse;
            thumbnail.DragDrop += Thumbnail_DragDrop;
            Controls.Add(frame);
        }

        private void Thumbnail_DragDrop(Control sender, DragDropEventArgs e)
        {
            if (e.DraggedControl.Tag is AssetInfo assetInfo)
            {
                var asset = Engine.Assets.Load(assetInfo.FullPath);
                if (asset != null)
                {
                    property.SetValue(asset);
                    thumbnail.Texture = AssetDatabase.GetThumbnail(asset);
                }
            }
        }

        private void Thumbnail_DragResponse(Control sender, DragDropEventArgs e)
        {
            var image = sender as ImageControl;
            if (e.DraggedControl.Tag is AssetInfo assetInfo)
            {
                image.Tint = ColorInt.ARGB(1, 0, 1, 0);
            }
            else
                image.Tint = -1;
        }

        private void Button_MouseClick(Control sender, MouseEventArgs args)
        {
            AssetBrowser.Open(Desktop, property.Type);
            AssetBrowser.SelectAsset(property.GetValue() as IAsset);
        }
    }
}
