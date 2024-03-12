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

    [PropertyControl(typeof(System.Enum))]
    public class EnumProperty : PropertyControl
    {
        public DropDownList Dropdown { get; private set; }

        public EnumProperty(GUIProperty property) : base(property)
        {
            Dropdown = new DropDownList();
            Dropdown.Padding = new Squid.Margin(0);
            Dropdown.Style = "textbox";
            Dropdown.Size = new Squid.Point(222, 32);
            Dropdown.Dock = DockStyle.Fill;
            Dropdown.Label.NoEvents = true;
            Dropdown.Button.NoEvents = true;

            Dropdown.StateChanged += () =>
            {
                Dropdown.Button.State = Dropdown.State;
                Dropdown.Label.State = Dropdown.State;
            };
            
            Dropdown.MouseClick += (sender, e) =>
            {
                if (Dropdown.IsOpen)
                    Dropdown.Close();
                else
                    Dropdown.Open();
            };

            Dropdown.Label.Style = "dropdownLabel";
            Dropdown.Label.Dock = DockStyle.Fill;//	 new Margin(4);
            //Dropdown.Button.Style = "textbox";
            Dropdown.Button.Size = new Point(24, 16);
            Dropdown.Button.Margin = new Margin(1, 0, 0, 0);
            Dropdown.Button.TextAlign = Alignment.MiddleCenter;
            //Dropdown.Button.Margin = new Margin(4, 8, 0, 8);
            Dropdown.Button.Dock = DockStyle.Right;//	 new Margin(4);
            Dropdown.Dropdown.Style = "window";
            Dropdown.Dropdown.Padding = new Squid.Margin(1);
            Dropdown.DropdownAutoSize = true;
            Dropdown.Listbox.Size = new Point(200, 32);
            //Dropdown.Listbox.Scrollbar.Margin = new Margin(0, 0, 0, 0);
            //Dropdown.Listbox.Scrollbar.Size = new Squid.Point(12, 12);
            //Dropdown.Listbox.Scrollbar.ButtonDown.Visible = false;
            //Dropdown.Listbox.Scrollbar.ButtonUp.Visible = false;
            //Dropdown.Listbox.Scrollbar.Slider.Margin = new Margin(0);
            //Dropdown.Listbox.Scrollbar.Slider.Style = "tooltip";
            //Dropdown.Listbox.Scrollbar.Slider.Button.Style = "scrollbar";

            Dropdown.Listbox.Scrollbar.Size = new Point(12, 16);
            Dropdown.Listbox.Scrollbar.ButtonDown.Visible = false;
            Dropdown.Listbox.Scrollbar.ButtonUp.Visible = false;
            Dropdown.Listbox.Scrollbar.Slider.Button.Margin = new Margin(2, 4, 0, 4);
            Dropdown.Listbox.Scrollbar.Slider.Ease = false;
            Dropdown.Listbox.Scrollbar.Slider.MinHandleSize = 64;
            Dropdown.Listbox.Scrollbar.Dock = DockStyle.Right;

            Dropdown.OnOpened += HandleDropdownOnOpened;

            object value = property.GetValue();
            ListBoxItem selected = null;

            foreach (object entry in Enum.GetValues(property.Type))
            {
                ListBoxItem item = new ListBoxItem();
                item.Text = entry.ToString().Replace("_", "");
                item.Value = entry;
                item.Style = "item";
                item.Size = new Point(32, 26);
                item.Margin = new Margin(0, 1, 0, 0);
                Dropdown.Items.Add(item);

                if (entry.Equals(value))
                    selected = item;
            }

            if (selected != null)
                Dropdown.SelectedItem = selected;

            Dropdown.SelectedItemChanged += new SelectedItemChangedEventHandler(Dropdown_SelectedItemChanged);

            ImageControl img = new ImageControl
            {
                Dock = DockStyle.Fill,
                NoEvents = true,
                Texture = "icon_down.png",
                Tiling = TextureMode.Center,
                
            };

            Dropdown.Button.GetElements().Add(img);
           
            Controls.Add(Dropdown);
        }

        void HandleDropdownOnOpened(Control sender, SquidEventArgs args)
        {
            DropDownList drop = sender as DropDownList;

            Window target = drop.Dropdown;
            //target.Position = drop.Location + new Point(drop.Size.x, 0);
            Point size = target.Size;

            //target.Size = new Point(size.x, 100);
            target.Opacity = 1;
            //target.Position += new Point(4, 4);
            //target.Actions.Add(new Fade { duration = 500, IsBlocking = false });
            //target.Actions.Add(new Resize(size, 500));
        }

        void Dropdown_SelectedItemChanged(Control sender, ListBoxItem value)
        {
            property.SetValue(value.Value);
        }
    }
}
