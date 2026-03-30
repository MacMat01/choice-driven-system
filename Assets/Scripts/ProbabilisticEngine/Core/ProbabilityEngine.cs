using System.Collections.Generic;
using System.Linq;
using ProbabilisticEngine.Interfaces;
using ProbabilisticEngine.Utils;

namespace ProbabilisticEngine.Core
{
    /// <summary>
    /// Versione generica del ProbabilityEngine.
    /// Gestisce un pool di ProbabilityItem e seleziona quelli validi.
    /// </summary>
    public class ProbabilityEngine<TState, TOption>
        where TState : IGameState
        where TOption : IProbabilityOption<TState>
    {
        private readonly List<ProbabilityItem<TState, TOption>> _items;

        public ProbabilityEngine(IEnumerable<ProbabilityItem<TState, TOption>> items)
        {
            _items = items.ToList();
        }

        /// <summary>
        /// Filtra i ProbabilityItem che hanno le condizioni rispettate
        /// e restituisce la lista di quelli validi.
        /// </summary>
        public List<ProbabilityItem<TState, TOption>> GetValidChoices(TState state)
        {
            return _items.Where(item => item.AreConditionsMet(state)).ToList();
        }
        
        public ProbabilityItem<TState, TOption> EvaluateRandom(TState state)
        {
            var validItems = GetValidChoices(state);
            
            if (validItems.Count == 0)
                return null;

            // Calcola i pesi per ogni choice valido
            var weights = validItems.Select(c => c.BaseWeight).ToList();
            
            // Seleziona un choice in base ai pesi
            int index = WeightedRandom.PickIndex(weights);
            var selectedItem = validItems[index];
            
            // Restituisce il ProbabilityItem selezionato (non valutato)
            return selectedItem;
        }
        
    }
}