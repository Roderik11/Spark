using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Editor
{
    public static class Selector
    {
        private static Entity _selectedObject;

        public static Entity SelectedObject
        {
            get { return _selectedObject; }
            set
            {
                _selectedObject = value;
                MessageDispatcher.Send("selectionChanged", SelectedObject);
            }
        }

        static Selector()
        {
        }

        public static void Draw()
        {
            if (SelectedObject != null)
            {
                foreach (var comp in SelectedObject.GetComponents())
                   if(comp is IDrawDebug drawDebug) drawDebug.DrawDebug();
            }
        }
    }
}
