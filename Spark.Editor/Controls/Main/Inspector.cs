using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Spark.Windows;
using Spark;
using System.Reflection;

namespace Spark.Editor
{
    public partial class Inspector : DockContent
    {
        private Dictionary<int, Type> Hash2 = new Dictionary<int, Type>();
        private Dictionary<int, Spark.Component> Hash = new Dictionary<int, Spark.Component>();
        private List<INotifyPropertyChanged> NotifyHooks = new List<INotifyPropertyChanged>();

        public static bool IsLocked;

        public Inspector()
        {
            InitializeComponent();

            Editor.SelectionChanged += Editor_SelectionChanged;
        }

        void Editor_SelectionChanged()
        {
            propertyGrid1.SelectedObject = Editor.SelectedObject;
        }
    }
}
