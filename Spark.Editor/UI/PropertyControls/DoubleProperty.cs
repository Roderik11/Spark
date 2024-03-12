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
    [PropertyControl(typeof(double))]
    public class DoubleProperty : PropertyControl
    {
        public TextBox Textbox { get; private set; }

        public DoubleProperty(GUIProperty property) : base(property)
        {
            Textbox = new TextBox();
            Textbox.Size = new Point(20, 20);
            Textbox.Dock = DockStyle.Fill;
            Textbox.Style = "textbox";
            Textbox.Mode = TextBoxMode.Numeric;
            Textbox.TextCommit += HandleTextboxOnTextCommit;

            Controls.Add(Textbox);
        }

        void HandleTextboxOnTextCommit(object sender, EventArgs e)
        {
            double v = Convert.ToDouble(Textbox.Text);
            double now = (double)property.GetValue();

            if (v != now)
            {
                property.SetValue(v);
                //ProtocolEntry.UpdateValue();
                NotifyChange();
            }
        }

        protected override void OnUpdate()
        {
            if (Desktop.FocusedControl == Textbox)
                return;

            Timer += Time.Delta;

            if (Timer > Interval)
            {
                Timer = 0;

                double value = (double)property.GetValue();

                string v = value.ToString();
                if (v != Textbox.Text)
                    Textbox.Text = v;
            }
        }
    }
}
