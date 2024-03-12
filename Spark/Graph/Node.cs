using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using SharpDX;

namespace Spark.Graph
{
    [Serializable]
    public abstract class Node
    {
        private readonly Dictionary<int, Port> HashedPorts = new Dictionary<int, Port>();

        [Browsable(false)]
        public int ID;

        [Browsable(false)]
        public Vector2 Position;

        [Browsable(false)]
        public bool Expanded;

        public readonly List<Port> Ports = new List<Port>();
        public readonly List<Port> Inputs = new List<Port>();
        public readonly List<Port> Outputs = new List<Port>();

        public NodeGraph Graph { get; private set; }

        public Port GetPort(string name)
        {
            int hash = name.GetHashCode();
            HashedPorts.TryGetValue(hash, out var port);
            return port;
        }

        public Port GetPort(int hash)
        {
            if (HashedPorts.TryGetValue(hash, out Port port))
                return port;
            return null;
        }

        public virtual object GetValue(Port port)
        {
            return default;
        }

        public void CreatePorts(NodeGraph graph)
        {
            Graph = graph;

            var inputs = Reflector.GetMapping<InputAttribute>(GetType());

            foreach (var field in inputs)
            {
                var att = field.GetAttribute<InputAttribute>();

                var port = new Port(this, field)
                {
                    Type = Port.InOut.Input,
                    Constraint = att.typeConstraint,
                    ConnectionType = att.connectionType,
                };

                Ports.Add(port);
                Inputs.Add(port);
                HashedPorts.Add(port.FieldHash, port);
            }

            var outputs = Reflector.GetMapping<OutputAttribute>(GetType());

            foreach (var field in outputs)
            {
                var att = field.GetAttribute<OutputAttribute>();

                var port = new Port(this, field)
                {
                    Type = Port.InOut.Output,
                    Constraint = att.typeConstraint,
                    ConnectionType = att.connectionType,
                };

                Ports.Add(port);
                Outputs.Add(port);
                HashedPorts.Add(port.FieldHash, port);
            }
        }
    }

    public enum ShowBackingValue
    {
        Never,
        Unconnected,
        Always
    }

    public enum ConnectionType
    {
        Multiple,
        Single,
    }

    public enum TypeConstraint
    {
        /// <summary> Allow all types of input</summary>
        None,

        /// <summary> Allow connections where input value type is assignable from output value type (eg. ScriptableObject --> Object)</summary>
        Inherited,

        /// <summary> Allow only similar types </summary>
        Strict,

        /// <summary> Allow connections where output value type is assignable from input value type (eg. Object --> ScriptableObject)</summary>
        InheritedInverse,

        /// <summary> Allow connections where output value type is assignable from input value or input value type is assignable from output value type</summary>
        InheritedAny
    }
}