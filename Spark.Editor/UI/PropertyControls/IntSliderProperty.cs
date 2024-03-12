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
    [PropertyControl(typeof(int))]
    public class IntSliderProperty : PropertyControl
    {
        public TextBox Textbox { get; private set; }
        public Slider Slider { get; private set; }

        public IntSliderProperty(GUIProperty property) : base(property)
        {
            Textbox = new TextBox();
            Textbox.Size = new Point(40, 20);
            Textbox.Dock = DockStyle.Left;
            Textbox.Style = "textbox";
            Textbox.Mode = TextBoxMode.Numeric;
            Controls.Add(Textbox);

            Slider = new Slider();
            Slider.Size = new Point(20, 20);
            Slider.Orientation = Orientation.Horizontal;
            Slider.Dock = DockStyle.Fill;
            Slider.Button.Style = "button";
            Slider.Button.Size = new Point(20, 20);
            Slider.Style = "tooltip";
            Slider.Minimum = -100;
            Slider.Maximum = 100;

            ValueRangeAttribute range = property.GetAttribute<ValueRangeAttribute>();
            if (range != null)
            {
                Slider.Minimum = range.Min;
                Slider.Maximum = range.Max;
                Slider.Steps = (range.Max - range.Min);
            }

            object value = property.GetValue();

            if (value != null)
            {
                Slider.Value = Convert.ToSingle(value);
                Textbox.Text = value.ToString();
            }

            Slider.ValueChanged += Slider_OnValueChanged;

            Controls.Add(Slider);
        }

        void Slider_OnValueChanged(Control sender)
        {
            property.SetValue(Convert.ToInt32(Slider.Value));
            Textbox.Text = Slider.Value.ToString("0");
        }
    }
}
