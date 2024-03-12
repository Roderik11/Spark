using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Shapes;
using AvalonDock;
using Spark;

namespace ED8000
{
    /// <summary>
    /// Interaction logic for Explorer.xaml
    /// </summary>
    public partial class ExplorerPanel : UserControl
    {
        public ExplorerPanel()
        {
            InitializeComponent();

            Editor.SelectionChanged += Editor_SelectionChanged;
        }

        void Editor_SelectionChanged()
        {
            // hack
            // Refresh();
        }

        public void Refresh()
        {
            treeview.Items.Clear();

            //foreach (Transform child in Entity.Root.Transform)
            //{
            //    Entity entity = child.Entity;
            //    TreeViewItem item = new TreeViewItem { Header = entity.Name, Tag = entity };
            //    item.VerticalContentAlignment = System.Windows.VerticalAlignment.Stretch;
               
            //    item.Items.Add(new TreeViewItem { Header = entity.Name, Tag = entity });

            //    treeview.Items.Add(item);
            //}
        }

        private void treeview_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null) return;

            TreeViewItem item = e.NewValue as TreeViewItem;
            Editor.SelectedObject = item.Tag;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }
    }
}
