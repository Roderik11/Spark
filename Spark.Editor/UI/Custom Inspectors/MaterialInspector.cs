using Squid;

namespace Spark.Editor
{
    [GUIInspector(typeof(Material))]
    public class MaterialInspector : GUIInspector
    {
        public MaterialInspector(GUIObject target) : base(target)
        {
            var cat = AddCategory(target.Name);
            
            //foreach (var prop in target.GetProperties())
            //    AddProperty(prop);

            var mat = target.Target as Material;

            foreach(var prop in mat.GetParams())
            {
                if (!prop.Value.Annotated) continue;
                var m = prop.Value.GetType().GetMember("Value")[0];
                var f = new Field(m);
                var p = new GUIProperty(f, prop.Value);
                AddProperty(p, prop.Key);
            }
        }
    }
}
