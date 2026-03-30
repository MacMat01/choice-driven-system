using System.Collections.Generic;
using ProbabilisticEngine.Interfaces;
using ProbabilisticEngine.Runtime;

namespace ProbabilisticEngine.Core
{
    public class ProbabilityOption : IProbabilityOption
    {
        public string Id { get; }
        
        public List<IEffect> Effects = new();
        public List<IModifier> Modifiers = new();
        

        /**
         *  TODO as future implementation
         */
        public float ApplyModifiers()
        {
            return 0f;
        }
        
        private float ApplyModifiers(float baseWeight, IGameState state)
        {
            return 0f; // TODO: implementare logica modificatori
        }
    }
}