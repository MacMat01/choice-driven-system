using ProbabilisticEngine.Runtime;

namespace ProbabilisticEngine.Interfaces
{
    public interface IEffect
    {
        void Apply(GameState state);
    }
}