using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Squid;
using Spark;
using System.Reflection;
using System.Deployment.Application;
using System.Runtime.InteropServices;
using Microsoft.SqlServer.Server;
using System.Data;
using static System.Net.Mime.MediaTypeNames;
using System.Net.Sockets;

namespace Spark.Editor
{
    public interface IPreview
    {
        void OnEnable();
        void OnDisable();
    }

    public class GUIInspector : Frame
    {
        static readonly Dictionary<Type, Type> memberTypeToControlType = new Dictionary<Type, Type>();
        static readonly Dictionary<Type, Type> typeToInspectorType = new Dictionary<Type, Type>();

        static GUIInspector()
        {
            var types = Reflector.GetTypes<PropertyControl>();

            foreach (var controlType in types)
            {
                var attr = controlType.GetCustomAttribute<PropertyControlAttribute>();
                if (attr == null) continue;
                if (attr.Type == null) continue;
                if (memberTypeToControlType.ContainsKey(attr.Type)) continue;

                memberTypeToControlType.Add(attr.Type, controlType);

                var subs = Reflector.GetTypes(attr.Type);
                foreach (var sub in subs)
                {
                    if (!memberTypeToControlType.ContainsKey(sub))
                        memberTypeToControlType.Add(sub, controlType);
                }
            }

            types = Reflector.GetTypes<GUIInspector>();

            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<GUIInspectorAttribute>();
                if (attr == null) continue;
                if (attr.Type == null) continue;
                if (typeToInspectorType.ContainsKey(attr.Type)) continue;

                typeToInspectorType.Add(attr.Type, type);

                var subs = Reflector.GetTypes(attr.Type);
                foreach (var sub in subs)
                {
                    if (!typeToInspectorType.ContainsKey(sub))
                        typeToInspectorType.Add(sub, type);
                }
            }
        }


        protected GUIObject target;

        private Frame lastExpander;

        public static int CaptionWidth = 160;

        public static GUIInspector GetInspector(GUIObject target, bool header = true)
        {
            if(FindControlType(target.Type, typeToInspectorType, out var inspectorType))
                return Activator.CreateInstance(inspectorType, target) as GUIInspector;

            return new GenericInspector(target, header);
        }

        public static PropertyControl GetPropertyControl(GUIProperty property)
        {
            var select = property.GetAttribute<ValueSelectAttribute>();
            if (select != null) 
                return new ValueSelectProperty(property);

            if (FindControlType(property.Type, memberTypeToControlType, out var controlType))
                return Activator.CreateInstance(controlType, property) as PropertyControl;

            return null;
        }

        private static bool FindControlType(Type type, Dictionary<Type, Type> dict, out Type result)
        {
            if (dict.TryGetValue(type, out result))
                return true;
                     
            if (dict.TryGetValue(type.BaseType, out result))
                return true;

            if(type.IsGenericType && dict.TryGetValue(type.GetGenericTypeDefinition(), out result))
                return true;

            if (type.BaseType.IsGenericType && dict.TryGetValue(type.BaseType.GetGenericTypeDefinition(), out result))
                return true;

            result = null;

            return false;
        }

        public GUIInspector(GUIObject target)
        {
            this.target = target;
            Dock = DockStyle.Top;
            AutoSize = AutoSize.Vertical;
            //Padding = new Margin(0, 0, 0, 2);
        }

        public virtual Control GetPreview() { return null; }

        public void FilterBy(string str)
        {
            str = str.ToLower();
            bool isempty = string.IsNullOrEmpty(str);
            bool anyVisible = false;

            foreach (var child in Controls)
            {
                if (child.Tag as string == "property")
                {
                    child.Visible = isempty || child.Name.ToLower().Contains(str.ToLower());
                    if (child.Visible) anyVisible = true;
                }
            }

            foreach (var child in Controls)
            {
                if (child.Tag as string == "category")
                    child.Visible = anyVisible;
            }

            PerformLayout();
        }

        public CategoryRow AddCategory(string text)
        {
            var row = new CategoryRow(text);
            Controls.Add(row);

            lastExpander = new Frame
            {
                Name = text,
                Dock = DockStyle.Top,
                AutoSize = AutoSize.Vertical,
            };
            
            row.Content = lastExpander;

            row.ExpandedChanged += (expanded) =>
            {
                row.Content.Visible = expanded;
            };

            Controls.Add(lastExpander);

            return row;
        }

        public void AddProperty(GUIProperty property, string text = null)
        {
            var item = GetInspectorElement(property, text);
            if (item == null) return;

            if (lastExpander != null)
                lastExpander.Controls.Add(item);
            else
                Controls.Add(item);
        }
        public void AddText(string label, string text)
        {
            var lbl = new Label
            {
                Text = text,
                Dock = DockStyle.Fill
            };
        
            AddRow(label, lbl);
        }

        public void AddRow(string label, Control control)
        {
            var item = GetInspectorElement(label, control);
            if (item == null) return;

            if (lastExpander != null)
                lastExpander.Controls.Add(item);
            else
                Controls.Add(item);
        }

        public void AddControl(Control item)
        {
            if (item == null) return;

            if (lastExpander != null)
                lastExpander.Controls.Add(item);
            else
                Controls.Add(item);
        }

        public static Control GetInspectorElement(GUIProperty property, string text = null)
        {
            var control = GetPropertyControl(property);
            if (control != null)
            {
                if (control.Expandable)
                    return control;

                return GetInspectorElement(property, control, text ?? property.Name);
            }

            if (!property.IsDictionary && 
                !property.Type.IsValueType && 
                 property.Type.HasAttribute<SerializableAttribute>(false))
            {
                var value = property.GetValue();
                if (value != null)
                {
                    var obj = new GUIObject(value);
                    obj.Depth = property.Depth;
                    obj.Index = property.Index;

                    return GetInspectorElement(obj, text ?? obj.Name);
                }
            }

            return null;
        }

        static Control GetInspectorElement(string label, Control control)
        {
            if (control == null) return null;

            var row = new GUIInspectorRow(false, false, label);
            row.Name = label;

            control.Padding = new Margin(8, 4, 8, 4);
            control.Dock = DockStyle.Fill;

            row.Content.Controls.Add(control);

            return row;
        }

        static Control GetInspectorElement(GUIProperty property, PropertyControl control, string label)
        {
            if (control == null) return null;

            var row = new GUIInspectorRow(control.Expandable, property.Expanded, label);
            row.Name = label;

            if (control.RowHeight != null)
                row.Size = new Point(32, (int)control.RowHeight);

            control.Padding = new Margin(8, 4, 8, 4);
            control.Dock = DockStyle.Fill;

            row.Content.Controls.Add(control);

            if(property.Depth > 0)
                row.Indent(property.Depth);

            return row;
        }

        static Control GetInspectorElement(GUIObject target, string name)
        {
            var row = new GUIInspectorRow(true, target.Expanded, name);
            row.Name = name;

            if (target.Depth > 0)
                row.Indent(target.Depth);

            Frame expanderRow = new Frame
            {
                Dock = DockStyle.Top,
                AutoSize = AutoSize.Vertical,
            };

            target.Depth ++;

            var control = GetInspector(target, false);
            control.Dock = DockStyle.Top;
            control.Padding = new Margin(0);
            control.AutoSize = AutoSize.Vertical;
            control.Visible = false;

            row.ExpandedChanged += (expanded) =>
            {
                target.Expanded = expanded;
                control.Visible = expanded;
            };

            expanderRow.Controls.Add(row);
            expanderRow.Controls.Add(control);
            control.Visible = target.Expanded;
            return expanderRow;
        }
    }


    public class CategoryRow : Frame
    {
        private Frame foldoutFrame;
        private Label lblName;
        public Frame iconFrame;

        public Frame Content;

        private ImageControl imgArrow;
        private bool expanded;

        public event Action<bool> ExpandedChanged;

        public CategoryRow(string name)
        {
            expanded = true;
            NoEvents = false;
            Size = new Point(32, 34);
            Padding = new Margin(0, 0, 0, 0);
            Margin = new Margin(0, 0, 0, 1);
            Dock = DockStyle.Top;
            Tag = "category";

            foldoutFrame = new Frame
            {
                Margin = new Margin(0, 0, 0, 0),
                Style = "category",
                Dock = DockStyle.Left,
                Size = new Point(26, 26),
            };

            lblName = new Label
            {
                NoEvents = true,
                Margin = new Margin(0, 0, 0, 0),
                Size = new Point(GUIInspector.CaptionWidth, 26),
                Style = "category",
                Dock = DockStyle.Fill,
                Text = name
            };

            iconFrame = new Frame
            {
                Margin = new Margin(1, 0, 0, 0),
                Size = new Point(32, 26),
                Style = "category",
                Dock = DockStyle.Right,
            };

            Controls.Add(foldoutFrame);
            Controls.Add(lblName);
            Controls.Add(iconFrame);

            imgArrow = new ImageControl
            {
                NoEvents = true,
                Texture = "icon_down.png",
                Size = new Point(26, 26),
                Dock = DockStyle.Fill,
                Tiling = TextureMode.Center
            };

            MouseClick += (s, e) =>
            {
                expanded = !expanded;
                imgArrow.Texture = expanded ? "icon_down.png" : "icon_right.png";
                ExpandedChanged?.Invoke(expanded);
            };

            foldoutFrame.Controls.Add(imgArrow);
        }

        protected override void OnStateChanged()
        {
            foldoutFrame.State = State;
            lblName.State = State;
            iconFrame.State = State;
        }
    }

    public class GUIInspectorRow : Frame
    {
        public readonly Button Foldout;
        public readonly Label Label;
        public readonly Frame Content;

        private readonly Frame foldoutFrame;
        private readonly Control indentFrame;
        private readonly Frame buttonFrame;

        public event Action<bool> ExpandedChanged;

        private bool isExpanded;

        public void Indent(int indent)
        {
            indentFrame.Visible = true;
            indentFrame.Size = new Point(26 * indent, indentFrame.Size.y);
            Label.Size = new Point(GUIInspector.CaptionWidth - 26 * indent, Label.Size.y);
        }

        public GUIInspectorRow(bool expandable, bool expanded, string label)
        {
            isExpanded = expanded;

            NoEvents = false;
            Size = new Point(32, 34);
            Padding = new Margin(0, 0, 0, 0);
            Margin = new Margin(0, 0, 0, 1);
            Dock = DockStyle.Top;
            Tag = "property";

            indentFrame = new Frame
            {
                Margin = new Margin(0, 0, 1, 0),
                Style = "propertyIndent",
                Dock = DockStyle.Left,
                Size = new Point(26, 26),
                Visible = false,
            };

            foldoutFrame = new Frame
            {
                Margin = new Margin(0, 0, 0, 0),
                Style = "propertyElement",
                Dock = DockStyle.Left,
                Size = new Point(26, 26),
            };

            Foldout = new Button
            {
                Margin = new Margin(0, 0, 0, 0),
                Style = expandable ? "foldout" : "",
                Dock = DockStyle.Fill,
                Checked = isExpanded
            };

            Label = new Label
            {
                NoEvents = true,
                Margin = new Margin(0, 0, 0, 0),
                Size = new Point(GUIInspector.CaptionWidth, 26),
                Style = "propertyElement",
                Dock = DockStyle.Left,
                Text = label
            };

            buttonFrame = new Frame
            {
                Margin = new Margin(1, 0, 0, 0),
                Size = new Point(32, 26),
                Style = "propertyElement",
                Dock = DockStyle.Right,
            };

            Content = new Frame
            {
                Margin = new Margin(1, 0, 0, 0),
                Style = "propertyElement",
                Dock = DockStyle.Fill,
            };

            foldoutFrame.Controls.Add(Foldout);

            Controls.Add(indentFrame);
            Controls.Add(foldoutFrame);
            Controls.Add(Label);
            Controls.Add(buttonFrame);
            Controls.Add(Content);

            MouseClick += (s, e) =>
            {
                isExpanded = !isExpanded;
                Foldout.Checked = isExpanded;
                ExpandedChanged?.Invoke(isExpanded);
            };
        }


        protected override void OnStateChanged()
        {
            indentFrame.State = State;
            foldoutFrame.State = State;
            Label.State = State;
            buttonFrame.State = State;
            Content.State = State;
        }
    }

}
