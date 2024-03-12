using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Client
{
    public static class Msg
    {
        public const string RefreshExplorer = "RefreshExplorer";
        public const string SelectionChanged = "SelectionChanged";
        public const string StartDockDrag = "StartDockDrag";
        public const string EndDockDrag = "EndDockDrag";
        public const string FocusEntity = "FocusEntity";
    }
}
