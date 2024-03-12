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
    public class PropertyControlAttribute : Attribute
    {
        public Type Type { get; private set; }

        public PropertyControlAttribute(Type type)
        {
            Type = type;
        }
    }
}
