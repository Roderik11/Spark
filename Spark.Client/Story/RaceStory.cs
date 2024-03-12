using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Spark.Client
{
    public class RaceStoryAttribute
    {
        public string Text { get; set; }
    }

    public class RaceStory
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            if (Description == null) return base.ToString();

            return Description;
        }
    }
}
