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
    [PropertyControl(typeof(List<>))]
    public class ListProperty : PropertyControl
    {
        //private ListElement selectedElement;
        private readonly GUIInspectorRow header;
        private readonly Frame expander;
        private readonly Label headerLabel;
        private Type elementBaseType;

        public ListProperty(GUIProperty property) : base(property)
        {
            Dock = DockStyle.Top;
            Expandable = true;
            AutoSize = AutoSize.Vertical;

            header = new GUIInspectorRow(true, property.Expanded, property.Name);
            if(property.Depth > 0)
                header.Indent(property.Depth);

            Controls.Add(header);

            expander = new Frame
            {
                Dock = DockStyle.Top,
                AutoSize = AutoSize.Vertical,
                Visible = property.Expanded
            };
            Controls.Add(expander);

            var btnPlus = new Button
            {
                Style = "iconplus",
                Size = new Point(32, 32),
                Margin = new Margin(1),
                Dock = DockStyle.Right,
            };

            header.Content.Controls.Add(btnPlus);

            headerLabel = new Label
            {
                NoEvents = true,
                Margin = new Margin(8, 0, 0, 0),
                Dock = DockStyle.Fill,
            };

            header.Content.Controls.Add(headerLabel);

            header.ExpandedChanged += (expanded) =>
            {
                property.Expanded = expanded;
                expander.Visible = expanded;
            };

            elementBaseType = property.GetElementType();

            for (int i = 0; i < property.GetArrayLength(); i++)
            {
                var element = property.GetArrayElementAtIndex(i);
                var elm = GUIInspector.GetInspectorElement(element, $"{element.Type.Name} {i}");

                //elm.MouseClick += (s, e) =>
                //{
                //    if (selectedElement != null)
                //        selectedElement.Selected = false;

                //    elm.Selected = true;
                //    selectedElement = elm;
                //};
                expander.Controls.Add(elm);
            }


            btnPlus.MouseClick += BtnPlus_MouseClick;

            UpdateHeaderText();
        }

        void UpdateHeaderText()
        {
            var count = property.GetArrayLength();
            var text = $"{count} Element";
            if (count != 1) text += "s";
            headerLabel.Text = text;
        } 
        private void BtnPlus_MouseClick(Control sender, MouseEventArgs args)
        {
            var element = property.AddElement();
            var elm = GUIInspector.GetInspectorElement(element, $"Element {element.Index}");
            expander.Controls.Add(elm);
            UpdateHeaderText();
        }

        protected override void OnUpdate()
        {
            //if (selectedElement == null) return;

            //if (Gui.GetButton(0) == ButtonState.Down)
            //{
            //    var hot = Desktop.HotControl;
            //    if (hot == null || !hot.IsChildOf(this))
            //    {
            //        selectedElement.Selected = false;
            //        selectedElement = null;
            //    }
            //}

            base.OnUpdate();
        }
    }
}
