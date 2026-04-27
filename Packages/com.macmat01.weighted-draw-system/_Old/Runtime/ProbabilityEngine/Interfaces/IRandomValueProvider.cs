using System;
namespace _Old.Runtime.ProbabilityEngine.Interfaces
{
    /// <summary>
    ///     Abstraction over random value generation to keep selection logic testable and replaceable.
    /// </summary>
    [Obsolete("Legacy _Old API. Use the new MacMat01.WeightedDrawSystem package APIs.", false)]
    public interface IRandomValueProvider
    {
        float NextFloat01();
        int   NextInt(int minInclusive, int maxExclusive);
    }
}
