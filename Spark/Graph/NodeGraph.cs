using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Spark.Graph
{

    public class NodeGraph : IOnDeserialize
    {
        public List<Node> Nodes = new List<Node>();
        public List<Connection> Connections = new List<Connection>();

        private readonly Dictionary<int, Node> hashedNodes = new Dictionary<int, Node>();
        private int nodeIDcounter;

        public NodeGraph()
        {

        }

        public void AddNode(Node node)
        {
            node.ID = Interlocked.Increment(ref nodeIDcounter);
            node.CreatePorts(this);
            Nodes.Add(node);
            hashedNodes.Add(node.ID, node);
        }

        public void RemoveNode(Node node)
        {
            foreach(var port in node.Ports)
                ClearConnections(port);
            Nodes.Remove(node);
            hashedNodes.Remove(node.ID);
        }

        private Connection GetConnection(Port a, Port b)
        {
            foreach (var connection in Connections)
            {
                bool check1 = connection.FromNodeID == a.Node.ID && connection.ToNodeID == b.Node.ID;
                bool check2 = connection.FromFieldHash == a.FieldHash && connection.FromFieldHash == b.FieldHash;
                if (check1 && check2) return connection;

                bool check3 = connection.FromNodeID == b.Node.ID && connection.ToNodeID == a.Node.ID;
                bool check4 = connection.FromFieldHash == b.FieldHash && connection.FromFieldHash == a.FieldHash;
                if (check3 && check4) return connection;
            }

            return null;
        }

        public bool IsConnected(Port port)
        {
            foreach (var connection in Connections)
            {
                if(connection.PortA == port) return true;
                if(connection.PortB == port) return true;
            }

            return false;
        }

        public bool IsConnected(Port a, Port b) => GetConnection(a, b) != null;

        public bool CanConnect(Port a, Port b)
        {
            if (a == b) return false;
            if (a.Node == b.Node) return false;
            if (a.Type == b.Type) return false;
            if (a.Graph != b.Graph) return false;
            if (a.Field.Type != b.Field.Type) return false;
            if (IsConnected(a, b)) return false;

            return true;
        }

        public void ClearConnections(params Port[] ports)
        {
            foreach (Port port in ports)
            {
                Connections.RemoveAll((x) => x.FromNodeID == port.Node.ID && x.FromFieldHash == port.FieldHash);
                Connections.RemoveAll((x) => x.ToNodeID == port.Node.ID && x.ToFieldHash == port.FieldHash);
            }
        }

        public void AddConnection(Port a, Port b)
        {
            if (a.ConnectionType == ConnectionType.Single)
                ClearConnections(a);

            if (b.ConnectionType == ConnectionType.Single)
                ClearConnections(b);

            Connections.Add(new Connection
            {
                PortA = a,
                PortB = b,
                FromNodeID = a.Node.ID,
                FromFieldHash = a.FieldHash,
                ToNodeID = b.Node.ID,
                ToFieldHash = b.FieldHash
            });
        }

        public void RemoveConnection(Port a, Port b)
        {
            var found = GetConnection(a, b);
            if (found != null)
                Connections.Remove(found);
        }

        public void OnDeserialize(JSON json)
        {
            int max = 0;
            foreach (var node in Nodes)
            {
                node.CreatePorts(this);
                hashedNodes.Add(node.ID, node);
                max = Math.Max(max, node.ID);
            }

            nodeIDcounter = max;

            foreach(var connection in Connections)
            {
                connection.PortA = hashedNodes[connection.FromNodeID].GetPort(connection.FromFieldHash);
                connection.PortB = hashedNodes[connection.ToNodeID].GetPort(connection.ToFieldHash);
            }
        }
    }
}
