﻿using System;
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


namespace Spark.Client
{
    [PropertyControl(typeof(double))]
    public class DoubleSliderProperty : PropertyControl
    {
        public TextBox Textbox { get; private set; }
        public Slider Slider { get; private set; }

        public DoubleSliderProperty(GUIProperty property) : base(property)
        {
            Textbox = new TextBox();
            Textbox.Size = new Point(40, 20);
            Textbox.Dock = DockStyle.Left;
            Textbox.Style = "textbox";
            Textbox.Mode = TextBoxMode.Numeric;
            Controls.Add(Textbox);

            Slider = new Slider();
            Slider.Margin = new Margin(8, 8, 8, 8);
            Slider.Size = new Point(16, 16);
            Slider.Orientation = Orientation.Horizontal;
            Slider.Dock = DockStyle.Fill;
            Slider.Button.Style = "scrollbar";
            Slider.Button.Size = new Point(16, 16);
            Slider.Style = "windowTitle";
            Slider.Minimum = -100;
            Slider.Maximum = 100;

            ValueRangeAttribute range = property.GetAttribute<ValueRangeAttribute>();
            if (range != null)
            {
                Slider.Minimum = range.Min;
                Slider.Maximum = range.Max;
            }

            object value = property.GetValue();

            if (value != null)
            {
                Slider.Value = Convert.ToSingle(value);
                Textbox.Text = Slider.Value.ToString();
            }

            Slider.ValueChanged += Slider_OnValueChanged;

            Controls.Add(Slider);
        }

        void Slider_OnValueChanged(Control sender)
        {
            property.SetValue(Slider.Value);
            Textbox.Text = Slider.Value.ToString();
        }
    }
}
