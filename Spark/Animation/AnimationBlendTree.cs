using System;
using System.Collections.Generic;
using SharpDX;

namespace Spark
{
    /// <summary>
    /// Blend Tree using 2D Cartesian Gradient Blending (Freeform Directional)
    /// </summary>
    public class AnimationBlendTree : AnimationState
    {
        public List<BlendLayer> Layers = new List<BlendLayer>();
        public string ParameterA;
        public string ParameterB;

        public override void Update(Animator animator)
        {
            timeElapsed += Time.SmoothDelta * Speed;

            UpdateWeights(animator);

            float maxWeight = 0;
            BlendLayer maxLayer = null;

            // find anim with highest weight
            foreach (var layer in Layers)
            {
                if (layer.Animation.Weight > maxWeight)
                {
                    maxWeight = layer.Animation.Weight;
                    maxLayer = layer;
                }
            }

            // update that anim
            maxLayer.Animation.Update(animator);

            // synchronize all active anims
            foreach (var layer in Layers)
            {
                if (layer.Animation.Weight <= 0) continue;
                if (layer == maxLayer) continue;

                layer.Animation.SetTimeNormalized(maxLayer.Animation.TimeNormalized);
            }
        }


        void UpdateWeights(Animator animator)
        {
            float valueA = animator.GetParam(ParameterA);
            float valueB = animator.GetParam(ParameterB);
            Vector2 input = new Vector2(valueA, valueB);
            Vector2 inputNormalized = Vector2.Normalize(input);

            float total_weight = 0.0f;
            int count = Layers.Count;
            float[] weights = new float[count];

            for (int i = 0; i < count; ++i)
            {
                // Calc vec i -> sample
                Vector2 point_i = Layers[i].values;
                Vector2 vec_is = input - point_i;

                float weight = 1.0f;

                for (int j = 0; j < count; ++j)
                {
                    if (j == i)
                        continue;

                    // Calc vec i -> j
                    Vector2 point_j = Layers[j].values;
                    Vector2 vec_ij = point_j - point_i;

                    // Calc Weight
                    float lensq_ij =  Vector2.Dot(vec_ij, vec_ij);
                    float new_weight = Vector2.Dot(vec_is, vec_ij) / lensq_ij;
                    if (lensq_ij == 0)
                        new_weight = 1;

                    new_weight = 1.0f - new_weight;
                    new_weight = MathUtil.Clamp(new_weight, 0.0f, 1.0f);

                    weight = Math.Min(weight, new_weight);
                }

                weights[i] = weight;
                total_weight += weight;
            }

            int maxWeightIndex = -1;
            float maxWeight = 0;
            float total = 1f / total_weight;

            for (int i = 0; i < count; ++i)
            {
                weights[i] = weights[i] * total;

                if(weights[i] > maxWeight)
                {
                    maxWeight = weights[i];
                    maxWeightIndex = i;
                }
            }

            if (maxWeightIndex > -1)
                weights[maxWeightIndex] = 1;

            for (int i = 0; i < count; ++i)
                Layers[i].Animation.Weight = weights[i] * Weight;

            Layers.Sort((a, b) => b.Animation.Weight.CompareTo(a.Animation.Weight));
        }

        public override int GetStateCount()
        {
            return Layers.Count;
        }

        public override AnimationState GetState(int index)
        {
            return Layers[index].Animation;
        }

        public class BlendLayer
        {
            public AnimationState Animation;
            public Vector2 values;
        }

    }
}
