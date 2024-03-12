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


namespace Spark.Editor
{
    [PropertyControl(typeof(Color3))]
    public class ColorProperty : PropertyControl
    {
        public Color3 Color;
        public Button Button { get; private set; }

        public ColorProperty(GUIProperty property) : base(property)
        {
            Color = (Color3)property.GetValue();

            Button = new Button();
            Button.Size = new Point(20, 20);
            Button.Dock = DockStyle.Fill;
            Button.Style = "color";
            Button.Margin = new Margin(0, 2, 0, 2);
            Button.Tint = Color.ToRgba();
            Controls.Add(Button);
        }
    }

    [PropertyControl(typeof(Vector4))]
    public class Color4Property : PropertyControl
    {
        public Color4 Color;
        public Button Button { get; private set; }

        public Color4Property(GUIProperty property) : base(property)
        {
            var vector = (Vector4)property.GetValue();
            Color = new Color4(vector);

            Button = new Button();
            Button.Size = new Point(20, 20);
            Button.Dock = DockStyle.Fill;
            Button.Style = "color";
            Button.Margin = new Margin(0, 2, 0, 2);
            Button.Tint = Color.ToRgba();
            Controls.Add(Button);
        }
    }
}
