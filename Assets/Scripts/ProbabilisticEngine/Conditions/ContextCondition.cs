using ProbabilisticEngine.Core;

namespace ProbabilisticEngine.Conditions
{
    public class ContextCondition : ICondition
    {
        public string Key;
        public string ExpectedValue;

        public bool Evaluate(GameState state)
        {
            return state.GetContext(Key) == ExpectedValue;
        }
    }
}