using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Spark;
using System.Reflection;

namespace ED8000
{
    /// <summary>
    /// Interaction logic for Inspector.xaml
    /// </summary>
    public partial class InspectorPanel : UserControl
    {
        public InspectorPanel()
        {
            InitializeComponent();

            Editor.SelectionChanged += Editor_SelectionChanged;
        }

        object _selection;

        void Editor_SelectionChanged()
        {
            _selection = Editor.SelectedObject;
            stackpanel.Children.Clear();

            if (_selection == null) return;

            Entity entity = _selection as Entity;

            foreach (Component component in entity.GetComponents())
            {
                Expander expander = new Expander();
                expander.Header = component.GetType().Name;
                
                PropertyGrid props = new PropertyGrid();
                props.SelectedObject = component;

                expander.Content = props;
                stackpanel.Children.Add(expander);
                expander.IsExpanded = true;
            }
        }
    }
}
