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

    [PropertyControl(typeof(Vector2))]
    public class Vector2Property : PropertyControl
    {
        public TextBox Textbox1 { get; private set; }
        public TextBox Textbox2 { get; private set; }

        public Vector2Property(GUIProperty property) : base(property)
        {
            // Size = new Point(100, 90);
            // RowHeight = 90;


            Textbox1 = new TextBox();
            Textbox1.Size = new Point(20, 26);
            Textbox1.Dock = DockStyle.Left;
            Textbox1.Style = "textbox";
            Textbox1.Margin = new Squid.Margin(0, 0, 0, 0);
            Textbox1.Mode = TextBoxMode.Numeric;

            Textbox2 = new TextBox();
            Textbox2.Size = new Point(20, 26);
            Textbox2.Dock = DockStyle.Fill;
            Textbox2.Style = "textbox";
            Textbox2.Margin = new Squid.Margin(4, 0, 0, 0);
            Textbox2.Mode = TextBoxMode.Numeric;

            object value = property.GetValue();

            if (value != null)
            {
                Vector2 v = (Vector2)value;

                Textbox1.Text = v.X.ToString("0.###");
                Textbox2.Text = v.Y.ToString("0.###");
            }

            Textbox1.TextCommit += Textbox_OnTextCommit;
            Textbox2.TextCommit += Textbox_OnTextCommit;

            Controls.Add(Textbox1);
            Controls.Add(Textbox2);

            // Margin = new Margin(-18, 0, 0, 0);
        }

        protected override void OnLayout()
        {
            base.OnLayout();

            int w = (Textbox2.Location.x + Textbox2.Size.x - Textbox1.Location.x - 4) / 2;
            Textbox1.Size = new Point(w, Textbox1.Size.y);
        }

        void Textbox_OnTextCommit(object sender, EventArgs e)
        {
            Vector2 v = new Vector2(Convert.ToSingle(Textbox1.Text), Convert.ToSingle(Textbox2.Text));

            object value = property.GetValue();
            Vector2 now = (Vector2)value;

            if (!v.Equals(now))
            {
                property.SetValue(v);
                //ProtocolEntry.UpdateValue();
                NotifyChange();
            }
        }

        protected override void OnUpdate()
        {
            if (Desktop.FocusedControl == Textbox1) return;
            if (Desktop.FocusedControl == Textbox2) return;

            Timer += Time.Delta;

            if (Timer > Interval)
            {
                Timer = 0;

                object value = property.GetValue();

                if (value != null)
                {
                    Vector2 v = (Vector2)value;

                    Textbox1.Text = v.X.ToString("0.###");
                    Textbox2.Text = v.Y.ToString("0.###");
                }
            }
        }
    }
}
