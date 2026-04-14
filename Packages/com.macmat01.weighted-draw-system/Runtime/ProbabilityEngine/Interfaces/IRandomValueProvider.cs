namespace ProbabilityEngine.Interfaces
{
    /// <summary>
    ///     Abstraction over random value generation to keep selection logic testable and replaceable.
    /// </summary>
    public interface IRandomValueProvider
    {
        float NextFloat01();
        int NextInt(int minInclusive, int maxExclusive);
    }
}

