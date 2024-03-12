using SharpDX;
using Squid;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;

using Point = Squid.Point;

namespace Spark.Editor
{
    public class PropertyFrame : Frame
    {
        public bool UseProtocol;

        static Dictionary<Type, Type> memberTypeToControlType = new Dictionary<Type, Type>();

        static PropertyFrame()
        {
            var types = Reflector.GetTypes<PropertyControl>();

            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<PropertyControlAttribute>();
                if (attr == null) continue;
                if (attr.Type == null) continue;

                if (!memberTypeToControlType.ContainsKey(attr.Type))
                {
                    memberTypeToControlType.Add(attr.Type, type);

                    var subs = Reflector.GetTypes(attr.Type);
                    foreach (var sub in subs)
                    {
                        if (!memberTypeToControlType.ContainsKey(sub))
                            memberTypeToControlType.Add(sub, type);
                    }
                }
            }
        }

        public void ToggleProperties(bool show)
        {
            foreach(var ctrl in Controls)
            {
                if(ctrl.Tag == null)
                    ctrl.Visible = show;
            }
        }

        public PropertyControl GetPropertyControl(GUIProperty field)
        {
            if (memberTypeToControlType.TryGetValue(field.Type, out Type found))
                return Activator.CreateInstance(found, field) as PropertyControl;
            else if (memberTypeToControlType.TryGetValue(field.Type.BaseType, out Type found2))
                return Activator.CreateInstance(found2, field) as PropertyControl;
            return null;
        }

        public void FilterBy(string str)
        {
            str = str.ToLower();
            bool isempty = string.IsNullOrEmpty(str);

            foreach (var child in Controls)
                child.Visible = isempty ? true : child.Name.ToLower().Contains(str);
        }

        public Label AddCategory(string text)
        {
            Label label = new Label();
            label.Tag = "category";
            label.Text = text;
            label.Style = "category";
            label.Dock = DockStyle.Top;
            label.Size = new Point(32, 26);
            label.Margin = new Margin(0, 0, 0, 4);
            Controls.Add(label);

            return label;
        }

        public void AddProperty(GUIProperty property)
        {
            var control = GetPropertyControl(property);
            if (control == null) return;

            Frame frame = new Frame();
            frame.Name = property.Name;
            frame.Size = new Point(32, 26);
            frame.Dock = DockStyle.Top;
            frame.Margin = new Margin(8, 0, 4, 2);
            frame.Style = "";
            Controls.Add(frame);

            Label label = new Label();
            label.NoEvents = true;
            label.Style = "label";
            label.Dock = DockStyle.Left;
            label.Size = new Point(100, 26);
            label.Text = property.Name;
            label.TextAlign = Alignment.MiddleLeft;
            frame.Controls.Add(label);

            if (control.RowHeight != null)
            {
                frame.Size = new Point(32, (int)control.RowHeight);
                label.TextAlign = Alignment.TopLeft;
            }

            control.Dock = DockStyle.Fill;
            frame.Controls.Add(control);
        }

        public void AddProperty(string name, object target)
        {
            if (target == null) return;

            Type type = target.GetType();
            Mapping map = Reflector.GetMapping(type);
            Field field = map.FirstOrDefault((f) => f.Name == name);

            if (field == null) return;

            var prop = new GUIProperty(field, target);

            AddProperty(prop);
        }

        //void HandleControlOnMemberChanged(ProtocolMemberValue entry)
        //{
        //    Terminal.Event(entry.Text);

        //    if (!UseProtocol) return;

        //    Sandbox.Protocol.Add(entry);
        //}

    }
}
