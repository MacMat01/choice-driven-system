using ProbabilisticEngine.Core;

namespace ProbabilisticEngine.Conditions
{
    public abstract class CustomConditionBase : ICondition
    {
        public abstract bool Evaluate(GameState state);
    }
}