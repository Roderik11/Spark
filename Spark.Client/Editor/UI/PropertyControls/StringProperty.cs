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
    [PropertyControl(typeof(string))]
    public class StringProperty : PropertyControl
    {
        public TextBox Textbox { get; private set; }

        public StringProperty(GUIProperty property) : base(property)
        {
            Textbox = new TextBox();
            Textbox.Size = new Point(20, 20);
            Textbox.Dock = DockStyle.Fill;
            Textbox.Style = "textbox";
            Textbox.TextCommit += HandleTextboxOnTextCommit;
            object value = property.GetValue();

            if (value != null)
                Textbox.Text = value.ToString();

            Controls.Add(Textbox);
        }

        void HandleTextboxOnTextCommit(object sender, EventArgs e)
        {
            try
            {
                string now = (string)property.GetValue();

                if (Textbox.Text != now)
                {
                    property.SetValue(Textbox.Text);
                    //ProtocolEntry.UpdateValue();
                    NotifyChange();
                }
            }
            catch { }
        }

        protected override void OnUpdate()
        {
            if (Desktop.FocusedControl == Textbox)
                return;

            Timer += Time.Delta;

            if (Timer > Interval)
            {
                Timer = 0;

                object value = property.GetValue();

                if (value != null)
                {
                    string v = value.ToString();
                    if (v != Textbox.Text)
                    {
                        Textbox.Text = v;
                    }
                }
            }
        }
    }
}
