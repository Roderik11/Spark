using System;
using System.Collections.Generic;
using SharpDX;

namespace Spark
{
    [Serializable]
    public class AnimationLayer
    {
        public string Name;
        public AnimationMask Mask;
        public float Weight;
        public List<AnimationState> States = new List<AnimationState>();
        public List<AnimationTransition> Transitions = new List<AnimationTransition>();

        public bool InTransition { get; private set; }
        public AnimationState CurrentState { get; private set; }
        public AnimationTransition CurrentTransition { get; private set; }

        private AnimationState returnToState;
        private float transitionTime;

        public int GetStateCount()
        {
            if (InTransition) return 2;
            return 1;
        }

        public AnimationState GetState(int index)
        {
            if(InTransition)
            {
                if (index == 0)
                    return CurrentTransition.Source;
                return CurrentTransition.Target;
            }

            if (CurrentState == null && States.Count > 0)
                CurrentState = States[0];

            return CurrentState;
        }

        public void Update(Animator animator)
        {
            if (CurrentState == null && States.Count > 0)
                CurrentState = States[0];

            if (InTransition)
            {
                transitionTime += Time.SmoothDelta;

                if (transitionTime > CurrentTransition.Duration)
                {
                    if (CurrentState != null)
                    {
                        CurrentState.Weight = 1;
                        CurrentState.Update(animator);
                    }

                    InTransition = false;
                    CurrentTransition = null;
                    return;
                }

                CurrentTransition.Source.Weight = 1;// - transitionTime / currentTransition.Duration;
                CurrentTransition.Target.Weight = Math.Min(1, transitionTime / CurrentTransition.Duration);

                CurrentTransition.Source.Update(animator);
                CurrentTransition.Target.Update(animator);
            }
            else
            {
                foreach (AnimationTransition transition in Transitions)
                {
                    if (transition.Source != CurrentState)
                        continue;

                    bool allConditionsMet = true;

                    if (transition.Conditions.Count == 0)
                        allConditionsMet = CurrentState.TimeNormalized >= .8f;

                    foreach (AnimationCondition condition in transition.Conditions)
                    {
                        if (condition.AutoReturn)
                        {
                            allConditionsMet = false;

                            if (CurrentState.TimeElapsed > CurrentState.Clip.DurationSeconds * condition.AutoThreshold)
                            {
                                InTransition = true;
                                transitionTime = 0;

                                CurrentTransition = transition;
                                CurrentTransition.Target = returnToState;

                                CurrentState = returnToState;
                                //CurrentState.TimeElapsed = 0;
                                returnToState = null;
                                Update(animator);
                                return;
                            }
                        }
                        else if (!condition.IsMet(animator))
                        {
                            allConditionsMet = false;
                            break;
                        }
                    }

                    if (allConditionsMet)
                    {
                        InTransition = true;
                        transitionTime = 0;

                        CurrentTransition = transition;
                        returnToState = CurrentState;

                        CurrentState = transition.Target;
                        CurrentState.SetTimeElapsed(0);
                        // currentTransition.Source.TimeElapsed;
                        //CurrentState.TimeNormalized = currentTransition.Source.TimeNormalized;
                        Update(animator);
                        return;
                    }
                }

                if (CurrentState != null)
                {
                    CurrentState.Weight = 1;
                    CurrentState.Update(animator);
                }
            }
        }
    }

}
