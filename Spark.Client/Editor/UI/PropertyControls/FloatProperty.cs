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


namespace Spark.Client
{
    [PropertyControl(typeof(float))]
    public class FloatProperty : PropertyControl
    {
        public TextBox Textbox { get; private set; }

        public FloatProperty(GUIProperty property) : base(property)
        {
            Textbox = new TextBox();
            Textbox.Size = new Point(20, 20);
            Textbox.Dock = DockStyle.Fill;
            Textbox.Style = "textbox";
            Textbox.TextCommit += HandleTextboxOnTextCommit;
            Textbox.Mode = TextBoxMode.Numeric;
            Controls.Add(Textbox);
        }

        void HandleTextboxOnTextCommit(object sender, EventArgs e)
        {
            float v = Convert.ToSingle(Textbox.Text);
            float now = (float)property.GetValue();

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

                float value = (float)property.GetValue();

                string v = value.ToString();
                if (v != Textbox.Text)
                    Textbox.Text = v;
            }
        }
    }
}
