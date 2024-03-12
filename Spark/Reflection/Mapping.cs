using System;
using System.Collections.Generic;

namespace Spark
{
    public class Mapping : List<Field>
    {
        private Dictionary<string, Field> cache;
    
        public void SetValue(string name, object instance, object value)
        {
            if(cache == null)
            {
                cache = new Dictionary<string, Field>();
                foreach (var item in this)
                    cache.Add(item.Name, item);
            }

            if (cache.TryGetValue(name, out var field))
                field.SetValue(instance, value);
        }
    }

    public class Mapping<T> : Mapping where T : Attribute
    {
    }

}