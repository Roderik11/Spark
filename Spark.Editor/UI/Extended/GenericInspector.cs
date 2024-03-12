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

namespace Spark.Editor
{
    public class GenericInspector : GUIInspector
    {
        public GenericInspector(GUIObject target, bool header = true):base(target)
        {
            if(header)
                AddCategory(target.Name);

            foreach (var prop in target.GetProperties())
                AddProperty(prop);
        }
    }
}
