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

namespace Spark.Client
{
    public class GUIInspectorAttribute : Attribute
    {
        public Type Type { get; private set; }

        public GUIInspectorAttribute(Type type)
        {
            Type = type;
        }
    }
}
