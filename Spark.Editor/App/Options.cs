using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;
using Spark;

namespace Spark.Editor
{
    public class Options
    {
        [Browsable(false)]
        public string LastProjectPath { get; set; }

        public void Save()
        {
            using (DataSerializer s = new DataSerializer())
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini"), s.SerializeXml(this, System.Xml.Formatting.Indented));
            }
        }
    }
}
