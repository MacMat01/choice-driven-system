using ProbabilisticEngine.Interfaces;
using ProbabilisticEngine.Runtime;

namespace ProbabilisticEngine.Conditions
{
    public class ResourceCondition<TState> : ICondition<TState> where TState : IReignState
    {
        public string Resource;
        public int MinValue;

        public bool Evaluate(GameState state)
        {
            return state.GetResource(Resource) >= MinValue;
        }

        public bool Evaluate(TState state) 
        {
            throw new System.NotImplementedException();
        }
    }
}