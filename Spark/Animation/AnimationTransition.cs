using System;
using System.Collections.Generic;
using SharpDX;

namespace Spark
{
    public enum ComparisonType
    {
        Equals, Greater, Smaller
    }


    public class AnimationTransition
    {
        public List<AnimationCondition> Conditions = new List<AnimationCondition>();
        public float Duration = .2f;
        public AnimationState Source;
        public AnimationState Target;
    }

    public class AnimationCondition
    {
        public bool AutoReturn;
        public float AutoThreshold = 0.89f;

        public ComparisonType Comparison;
        public string Parameter;
        public float Value;

        public bool IsMet(Animator animator)
        {
            if (Comparison == ComparisonType.Smaller)
                return animator.GetParam(Parameter) < Value;
            else if (Comparison == ComparisonType.Greater)
                return animator.GetParam(Parameter) > Value;
            else
                return animator.GetParam(Parameter) == Value;
        }
    }
}
