using System.Collections.Generic;
using JetBrains.Annotations;
using ProbabilityEngine.Interfaces;
namespace ProbabilityEngine.Core
{
    /// <summary>
    ///     Fully generic ProbabilityItem.
    ///     Stores any payload type and evaluates optional conditions against a game state.
    /// </summary>
    public class ProbabilityItem<TState, TValue>
        where TState : IGameState
    {
        public float BaseWeight;

        [CanBeNull] public List<ICondition<TState>> Conditions;
        public string Id;
        public TValue Value;

        /// <summary>
        ///     Checks whether all conditions are satisfied for the given state.
        /// </summary>
        public bool AreConditionsMet(TState state)
        {
            if (Conditions == null)
            {
                return true;
            }

            foreach (ICondition<TState> c in Conditions)
                if (!c.Evaluate(state))
                {
                    return false;
                }

            return true;
        }
    }
}
