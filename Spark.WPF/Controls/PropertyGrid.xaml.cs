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
using System.Reflection;
using Spark;

namespace ED8000
{
    /// <summary>
    /// Interaction logic for PropertyGrid.xaml
    /// </summary>
    public partial class PropertyGrid : UserControl
    {
        public PropertyGrid()
        {
            InitializeComponent();
        }

        object _selection;

        public object SelectedObject
        {
            get { return _selection; }
            set
            {
                _selection = value;
                Refresh();
            }
        }

        private void Refresh()
        {
            grid.Children.Clear();
            grid.RowDefinitions.Clear();

            grid.Children.Add(gridSplitter1);
            Grid.SetColumn(gridSplitter1, 1);

            if (_selection == null) return;

            PropertyInfo[] infos = Reflector.GetProperties(_selection.GetType());

            int row = 0;
            foreach (PropertyInfo info in infos)
            {
                if (!info.CanWrite) continue;
                if (info.GetSetMethod(false) == null) continue;

                Button btn = new Button();
                btn.Height = 16; btn.Width = 16;
                grid.Children.Add(btn);
                Grid.SetColumn(btn, 0);
                Grid.SetRow(btn, row);

                Label label1 = new Label();
                label1.Content = info.Name;
                grid.Children.Add(label1);
                Grid.SetColumn(label1, 1);
                Grid.SetRow(label1, row);
              
                TextBox label2 = new TextBox();
                label2.Text = info.Name;
                grid.Children.Add(label2);
                Grid.SetColumn(label2, 2);
                Grid.SetRow(label2, row);

                grid.RowDefinitions.Add(new RowDefinition());

                row++;
            }

            if(row > 0)
                Grid.SetRowSpan(gridSplitter1, row);
        }
    }
}
