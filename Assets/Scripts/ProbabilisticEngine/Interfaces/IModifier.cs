using ProbabilisticEngine.Runtime;

namespace ProbabilisticEngine.Interfaces
{
    public interface IModifier
    {
        float Apply(float currentWeight, GameState state);
    }
}