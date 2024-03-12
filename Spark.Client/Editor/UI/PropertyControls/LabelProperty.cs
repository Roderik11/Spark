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

    public class LabelProperty : PropertyControl
    {
        public Label Label { get; private set; }

        public LabelProperty(GUIProperty property) : base(property)
        {
            Label = new Label();
            Label.Size = new Point(20, 20);
            Label.Dock = DockStyle.Fill;

            object value = property.GetValue();

            if (value != null)
                Label.Text = value.ToString();

            Controls.Add(Label);
        }

        protected override void OnUpdate()
        {
            Timer += Time.Delta;

            if (Timer > Interval)
            {
                Timer = 0;

                object value = property.GetValue();

                if (value != null)
                {
                    string v = value.ToString();
                    if (v != Label.Text)
                    {
                        Label.Text = v;
                    }
                }
            }
        }
    }
}
