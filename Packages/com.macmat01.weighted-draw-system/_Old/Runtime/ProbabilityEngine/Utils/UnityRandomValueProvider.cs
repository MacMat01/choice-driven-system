using System;
using _Old.Runtime.ProbabilityEngine.Interfaces;
using Random = UnityEngine.Random;

namespace _Old.Runtime.ProbabilityEngine.Utils
{
    [Obsolete("Legacy _Old API. Use the new MacMat01.WeightedDrawSystem package APIs.", false)]
    public sealed class UnityRandomValueProvider : IRandomValueProvider
    {
        public static readonly UnityRandomValueProvider Shared = new UnityRandomValueProvider();

        private UnityRandomValueProvider()
        {
        }

        public float NextFloat01()
        {
            return Random.value;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            return Random.Range(minInclusive, maxExclusive);
        }
    }
}
