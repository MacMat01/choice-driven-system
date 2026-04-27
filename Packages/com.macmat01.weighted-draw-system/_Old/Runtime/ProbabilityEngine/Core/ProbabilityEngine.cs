using System;
using System.Collections.Generic;
using System.Linq;
using _Old.Runtime.ProbabilityEngine.Interfaces;
using _Old.Runtime.ProbabilityEngine.Utils;
namespace _Old.Runtime.ProbabilityEngine.Core
{
    /// <summary>
    ///     Generic version of the ProbabilityEngine.
    ///     Manages a pool of ProbabilityItem instances and selects valid entries.
    /// </summary>
    [Obsolete("Legacy _Old API. Use the new MacMat01.WeightedDrawSystem package APIs.", false)]
    public class ProbabilityEngine<TState, TValue>
        where TState : IGameState
    {
        private readonly List<ProbabilityItem<TState, TValue>> items;
        private readonly IRandomValueProvider randomValueProvider;

        public ProbabilityEngine(IEnumerable<ProbabilityItem<TState, TValue>> items, IRandomValueProvider randomValueProvider = null)
        {
            this.items = items != null ? items.ToList() : new List<ProbabilityItem<TState, TValue>>();
            this.randomValueProvider = randomValueProvider ?? UnityRandomValueProvider.Shared;
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
                int uniformIndex = randomValueProvider.NextInt(0, validItems.Count);
                return validItems[uniformIndex];
            }

            // Select an item based on weighted randomness.
            int index = WeightedRandom.PickIndex(weights, randomValueProvider);
            ProbabilityItem<TState, TValue> selectedItem = validItems[index];

            // Return the selected ProbabilityItem (option effects are not applied here).
            return selectedItem;
        }
    }
}
