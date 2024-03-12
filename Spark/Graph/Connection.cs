using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Graph
{
    [Serializable]
    public class Connection
    {
        public int FromNodeID;
        public int ToNodeID;

        public int FromFieldHash;
        public int ToFieldHash;

        public Port PortA;
        public Port PortB;
    }

}
