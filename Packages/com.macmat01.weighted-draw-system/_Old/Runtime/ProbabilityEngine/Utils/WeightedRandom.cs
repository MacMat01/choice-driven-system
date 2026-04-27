using System;
using System.Collections.Generic;
using System.Linq;
using _Old.Runtime.ProbabilityEngine.Interfaces;
namespace _Old.Runtime.ProbabilityEngine.Utils
{
    [Obsolete("Legacy _Old API. Use the new MacMat01.WeightedDrawSystem package APIs.", false)]
    public static class WeightedRandom
    {
        public static int PickIndex(List<float> weights)
        {
            return PickIndex((IReadOnlyList<float>)weights);
        }

        public static int PickIndex(IReadOnlyList<float> weights, IRandomValueProvider randomValueProvider = null)
        {
            if (weights == null || weights.Count == 0)
            {
                return -1;
            }

            IRandomValueProvider randomProvider = randomValueProvider ?? UnityRandomValueProvider.Shared;
            float total = weights.Sum();

            float r = randomProvider.NextFloat01() * total;

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
