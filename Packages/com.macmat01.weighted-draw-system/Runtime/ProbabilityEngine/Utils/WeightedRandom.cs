using System.Collections.Generic;
using System.Linq;
using ProbabilityEngine.Interfaces;
namespace ProbabilityEngine.Utils
{
    public static class WeightedRandom
    {
        public static int PickIndex(List<float> weights)
        {
            return PickIndex((IReadOnlyList<float>)weights, null);
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
