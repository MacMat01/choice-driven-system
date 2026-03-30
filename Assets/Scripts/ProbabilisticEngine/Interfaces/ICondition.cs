using ProbabilisticEngine.Runtime;

namespace ProbabilisticEngine.Interfaces
{
    public interface ICondition<TState> where TState : IGameState
    {
        bool Evaluate(TState state);
    }
}