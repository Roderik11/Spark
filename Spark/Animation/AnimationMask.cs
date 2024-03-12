using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark
{
    public class AnimationMask : Asset
    {
        public List<string> Bones = new List<string>();

        public bool Contains(string name)
        {
            return Bones.Contains(name);
        }
    }
}
