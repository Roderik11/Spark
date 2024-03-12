using System;

namespace Spark
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MapComponentAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class AllowMultipleAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class ExecuteInEditorAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RequiresAttribute : Attribute
    {
        public Type[] Types;

        public RequiresAttribute(params Type[] componentType)
        {
            Types = componentType;
        }
    }

    public class ValueRangeAttribute : Attribute
    {
        public float Min;
        public float Max;
        public float Step = 1;

        public ValueRangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public ValueRangeAttribute(float min, float max, float step)
        {
            Min = min;
            Max = max;
            Step = step;
        }
    }
}