using UnityEngine;
namespace WeightedDraw
{
    public interface IRandomValueProvider
    {
        float NextFloat(float minInclusive, float maxExclusive);
        int   NextInt(int minInclusive, int maxExclusive);
    }

    public sealed class UnityRandomValueProvider : IRandomValueProvider
    {
        public static readonly UnityRandomValueProvider Shared = new UnityRandomValueProvider();

        private UnityRandomValueProvider()
        {
        }

        public float NextFloat(float minInclusive, float maxExclusive)
        {
            return Random.Range(minInclusive, maxExclusive);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            return Random.Range(minInclusive, maxExclusive);
        }
    }
}
