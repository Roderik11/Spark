using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Graph
{
    [AttributeUsage(AttributeTargets.Field)]
    public class InputAttribute : Attribute
    {
        public ShowBackingValue backingValue;
        public ConnectionType connectionType;
        public TypeConstraint typeConstraint;

        public InputAttribute(ShowBackingValue backingValue = ShowBackingValue.Unconnected, ConnectionType connectionType = ConnectionType.Single, TypeConstraint typeConstraint = TypeConstraint.None)
        {
            this.backingValue = backingValue;
            this.connectionType = connectionType;
            this.typeConstraint = typeConstraint;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class OutputAttribute : Attribute
    {
        public ShowBackingValue backingValue;
        public ConnectionType connectionType;
        public TypeConstraint typeConstraint;

        public OutputAttribute(ShowBackingValue backingValue = ShowBackingValue.Unconnected, ConnectionType connectionType = ConnectionType.Multiple, TypeConstraint typeConstraint = TypeConstraint.None)
        {
            this.backingValue = backingValue;
            this.connectionType = connectionType;
            this.typeConstraint = typeConstraint;
        }
    }

}
