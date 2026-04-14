using ProbabilityEngine.Interfaces;
using Random = UnityEngine.Random;

namespace ProbabilityEngine.Utils
{
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

