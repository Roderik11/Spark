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
    [PropertyControl(typeof(bool))]
    public class BoolProperty : PropertyControl
    {
        public Button CheckButton { get; private set; }
        public ImageControl Image { get; private set; }

        public BoolProperty(GUIProperty property) : base(property)
        {
            CheckButton = new Button();
            CheckButton.Dock = DockStyle.Left;
            CheckButton.Size = new Point(18, 24);
            CheckButton.CheckOnClick = true;
            CheckButton.CheckedChanged += CheckBox_CheckedChanged;
            CheckButton.Style = "checkbox";
            CheckButton.Margin = new Margin(0, 4, 4, 4);
            Controls.Add(CheckButton);

            Image = new ImageControl();
            Image.Dock = DockStyle.Fill;
            Image.Margin = new Margin(4);
            Image.Style = "checkmark";
            Image.NoEvents = true;
            CheckButton.GetElements().Add(Image);

            CheckButton.CheckedChanged += CheckBox_CheckedChanged;

            object value = property.GetValue();

            if (value != null)
                CheckButton.Checked = (bool)value;

            Image.Visible = CheckButton.Checked;
        }

        void CheckBox_CheckedChanged(Control sender)
        {
            property.SetValue(CheckButton.Checked);
            Image.Visible = CheckButton.Checked;
            //Label.Text = CheckButton.Checked ? "enabled" : "disabled";
        }

        protected override void OnUpdate()
        {
            Timer += Time.Delta;
            if (Timer > Interval)
            {
                Timer = 0;

                object value = property.GetValue();

                if (value != null)
                    CheckButton.Checked = (bool)value;
            }
        }
    }

    //[PropertyControl(typeof(bool))]
    public class BoolProperty_NEW : PropertyControl
    {
        public Button CheckButton { get; private set; }

        public BoolProperty_NEW(GUIProperty property) : base(property)
        {
            CheckButton = new Button();
            CheckButton.Dock = DockStyle.Left;
            CheckButton.Size = new Point(40, 24);
            CheckButton.CheckOnClick = true;
            CheckButton.CheckedChanged += CheckBox_CheckedChanged;
            CheckButton.Style = "switch";
            CheckButton.Margin = new Margin(0, 2, 2, 2);
            Elements.Add(CheckButton);

            CheckButton.CheckedChanged += CheckBox_CheckedChanged;

            object value = property.GetValue();

            if (value != null)
                CheckButton.Checked = (bool)value;
        }

        void CheckBox_CheckedChanged(Control sender)
        {
            property.SetValue(CheckButton.Checked);
            //CheckButton.Text = CheckButton.Checked ? "On" : "Off";
        }

        protected override void OnUpdate()
        {
            Timer += Time.Delta;
            if (Timer > Interval)
            {
                Timer = 0;

                object value = property.GetValue();

                if (value != null)
                    CheckButton.Checked = (bool)value;
            }
        }
    }
}
