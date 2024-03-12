using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark
{
    public interface ISerialize
    {
        JSON ToJSON();
        void FromJSON(JSON json);
    }

    public interface IOnDeserialize
    {
        void OnDeserialize(JSON json);
    }
}
