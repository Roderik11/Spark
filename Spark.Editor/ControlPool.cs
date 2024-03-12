using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Spark.Editor
{
    public static class ControlPool
    {
        private static Dictionary<string, Stack<Control>> Cache = new Dictionary<string, Stack<Control>>();

        public static T GetControl<T>() where T : Control
        {
            Type type = typeof(T);

            if (!Cache.ContainsKey(type.Name))
                Cache.Add(type.Name, new Stack<Control>());

            if (Cache[type.Name].Count > 0)
                return Cache[type.Name].Pop() as T;

            return Activator.CreateInstance<T>();
        }

        public static void ReleaseControl(Control control)
        {
            if (control == null) return;

            Type type = control.GetType();

            if (!Cache.ContainsKey(type.Name))
                Cache.Add(type.Name, new Stack<Control>());

            Cache[type.Name].Push(control);
        }
    }
}
