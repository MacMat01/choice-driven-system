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
        where TOption : IProbabilityOption
    {
        private readonly List<ProbabilityItem<TState, TOption>> _choices;

        public ProbabilityEngine(IEnumerable<ProbabilityItem<TState, TOption>> choices)
        {
            _choices = choices.ToList();
        }

        /// <summary>
        /// Filtra i ProbabilityItem che hanno le condizioni rispettate
        /// e restituisce la lista di quelli validi.
        /// </summary>
        public List<ProbabilityItem<TState, TOption>> GetValidChoices(TState state)
        {
            return _choices.Where(choice => choice.AreConditionsMet(state)).ToList();
        }
        
        public ProbabilityItem<TState, TOption> EvaluateRandom(TState state)
        {
            var validChoices = GetValidChoices(state);
            
            if (validChoices.Count == 0)
                return null;

            // Calcola i pesi per ogni choice valido
            var weights = validChoices.Select(c => c.BaseWeight).ToList();
            
            // Seleziona un choice in base ai pesi
            int index = WeightedRandom.PickIndex(weights);
            var selectedChoice = validChoices[index];
            
            // Restituisce il ProbabilityItem selezionato (non valutato)
            return selectedChoice;
        }
        
    }
}