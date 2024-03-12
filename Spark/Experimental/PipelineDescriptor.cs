using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spark
{
    public class TestPipeline
    {

    }

    public class PipelineDescriptor
    {
        public class Stage
        {
            public string Name;
        }
    }

    public abstract class PipelineCommand
    {
        public virtual void Execute() { }
    }

    public class ClearTarget : PipelineCommand
    {
        public bool Depth;
        public bool Color;
    }

    public class SwitchTarget : PipelineCommand
    {
        public string Target;
    }

    public class DrawGeometry : PipelineCommand
    {
        public int Flags;
    }

    public class BindBuffer : PipelineCommand
    {
        public string Target;
        public int Index;
        public int Slot;
    }
}
