using System.Collections.Generic;
using System.Linq;
using ProbabilityEngine.Interfaces;
using ProbabilityEngine.Utils;
using Random = UnityEngine.Random;
namespace ProbabilityEngine.Core
{
    /// <summary>
    ///     Generic version of the ProbabilityEngine.
    ///     Manages a pool of ProbabilityItem instances and selects valid entries.
    /// </summary>
    public class ProbabilityEngine<TState, TValue>
        where TState : IGameState
    {
        private readonly List<ProbabilityItem<TState, TValue>> items;

        public ProbabilityEngine(IEnumerable<ProbabilityItem<TState, TValue>> items)
        {
            this.items = items != null ? items.ToList() : new List<ProbabilityItem<TState, TValue>>();
        }

        /// <summary>
        ///     Filters ProbabilityItem entries whose conditions are satisfied
        ///     and returns the list of valid items.
        /// </summary>
        public List<ProbabilityItem<TState, TValue>> GetValidChoices(TState state)
        {
            return items.Where(item => item != null && item.AreConditionsMet(state)).ToList();
        }

        public ProbabilityItem<TState, TValue> EvaluateRandom(TState state)
        {
            List<ProbabilityItem<TState, TValue>> validItems = GetValidChoices(state);

            if (validItems.Count == 0)
            {
                return null;
            }

            List<float> weights = validItems.Select(static item => item.BaseWeight > 0f ? item.BaseWeight : 0f).ToList();
            float totalWeight = weights.Sum();
            if (totalWeight <= 0f)
            {
                int uniformIndex = Random.Range(0, validItems.Count);
                return validItems[uniformIndex];
            }

            // Select an item based on weighted randomness.
            int index = WeightedRandom.PickIndex(weights);
            ProbabilityItem<TState, TValue> selectedItem = validItems[index];

            // Return the selected ProbabilityItem (option effects are not applied here).
            return selectedItem;
        }
    }
}
