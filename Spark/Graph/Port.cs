using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Graph
{
    public class Port
    {
        public enum InOut
        {
            Input = 0,
            Output = 1
        }

        public InOut Type;
        public TypeConstraint Constraint;
        public ConnectionType ConnectionType;
        public Field Field;
        public int FieldHash;
        public Node Node;
        public object Tag;

        public NodeGraph Graph => Node.Graph;

        public bool IsConnected => Graph.IsConnected(this);
        public Port(Node node, Field field)
        {
            Node = node;
            Field = field;
            FieldHash = field.Name.GetHashCode();
        }
    }
}
