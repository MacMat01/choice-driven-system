using System.Collections.Generic;
using UnityEngine;
namespace ProbabilityEngine.Utils
{
    public static class WeightedRandom
    {
        public static int PickIndex(List<float> weights)
        {
            float total = 0f;
            foreach (float w in weights)
            {
                total += w;
            }

            float r = Random.value * total;

            for (int i = 0; i < weights.Count; i++)
            {
                if (r < weights[i])
                {
                    return i;
                }

                r -= weights[i];
            }

            return weights.Count - 1;
        }
    }
}
