using System.Collections.Generic;
using JetBrains.Annotations;

namespace ProbabilisticEngine.Data
{
    [System.Serializable]
    public class OptionDefinition
    {
        public string Id;
        [CanBeNull] public List<EffectDefinition> Effects;
        [CanBeNull] public List<ModifierDefinition> Modifiers;
    }
}