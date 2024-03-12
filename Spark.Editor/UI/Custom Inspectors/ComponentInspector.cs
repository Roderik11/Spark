using Squid;
using Spark;
using System.Reflection;
using System.Deployment.Application;
using System.Runtime.InteropServices;

namespace Spark.Editor
{
    [GUIInspector(typeof(Component))]
    public class ComponentInspector : GUIInspector
    {
        public ComponentInspector (GUIObject target):base (target)
        {
            var cat = AddCategory(target.Name);

            bool needsCheckbox = typeof(IUpdate).IsAssignableFrom(target.Type) || typeof(IDraw).IsAssignableFrom(target.Type);
            
            if (needsCheckbox)
            {
                var sp = target.GetProperty("Enabled");
                var enabled = new BoolProperty(sp)
                {
                    Size = new Point(18, 18),
                    Margin = new Margin(8, 9, 8, 9),
                    Dock = DockStyle.Fill,
                };
                enabled.CheckButton.Dock = DockStyle.Fill;
                enabled.CheckButton.Margin = new Margin(0);
                enabled.Image.Dock = DockStyle.Fill;
                enabled.Image.Margin = new Margin(3);

                cat.iconFrame.Controls.Add(enabled);
            }

            foreach (var prop in target.GetProperties())
                AddProperty(prop);
        }
    }

    [GUIInspector(typeof(AssetReader<>))]
    public class AssetInspector : GUIInspector
    {
        public AssetInspector(GUIObject target) : base(target)
        {
            AddCategory("Import Settings");

            foreach (var prop in target.GetProperties())
                AddProperty(prop);
        }
    }
}
