using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squid;
using Spark;
using System.Reflection;
using System.IO;

namespace Spark.Client
{
    public class SettingsControl : ScrollPanel
    {
        public SettingsControl()
        {
            Style = "frame";
            var obj = new GUIObject(Engine.Settings);
            var inspector = GUIInspector.GetInspector(obj);
            Content.Controls.Add(inspector);
        }
    }
}
