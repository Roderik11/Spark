using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark
{
    public interface IAsset
    {
        Guid Id { get; }
        string Name { get; }
        string Path { get; }
    }
}
