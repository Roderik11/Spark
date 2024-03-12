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
    [PropertyControl(typeof(int))]
    public class IntProperty : PropertyControl
    {
        public TextBox Textbox { get; private set; }

        public IntProperty(GUIProperty property) : base(property)
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
            int v = Convert.ToInt32(Textbox.Text);
            int now = (int)property.GetValue();

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

                int value = (int)property.GetValue();

                string v = value.ToString();
                if (v != Textbox.Text)
                {
                    Textbox.Text = v;
                }
            }
        }
    }
}
