namespace ProbabilityEngine.Interfaces
{
    public interface ICondition<in TState> where TState : IGameState
    {
        bool Evaluate(TState state);
    }
}
