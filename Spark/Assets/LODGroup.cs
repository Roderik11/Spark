using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark
{
    public class LODGroup : Asset
    {
        public List<float> Ranges = new List<float>();

        public LODGroup() { }
        public LODGroup(string name, params float[] ranges)
        {
            Name = name;
            Ranges = ranges.ToList();
        }
    }

    public static class LODGroups
    {
        public static LODGroup Trees;
        public static LODGroup LargeProps;
        public static LODGroup SmallProps;

        static LODGroups()
        {
            //Trees = new LODGroup("Trees", 32, 64, 128, 256, 512, 1024);
            //LargeProps = new LODGroup("LargeProps", 32, 64, 128, 256, 512, 1024);
            //SmallProps = new LODGroup("SmallProps", 32, 64, 128, 256, 512, 1024);

            Trees = new LODGroup("Trees", .5f, .4f, .3f, .2f, .1f, .0f);
            LargeProps = new LODGroup("LargeProps", .6f, .3f, .1f, .05f);
            SmallProps = new LODGroup("SmallProps", 32, 64, 128, 256, 512, 1024);
        }
    }
}
