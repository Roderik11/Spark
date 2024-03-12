using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark
{
    public abstract class NetComponent : Component
    {
        public bool isClient;
        public bool isServer;
        public bool isLocalPlayer;
    }

    public sealed class NetIdentity : Component
    {
        public int NetworkId;
    }
}
