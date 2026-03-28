using ProbabilisticEngine.Core;

namespace ProbabilisticEngine.Conditions
{
    public class FlagCondition : ICondition
    {
        public string Flag;

        public bool Evaluate(GameState state)
        {
            return state.HasFlag(Flag);
        }
    }
}