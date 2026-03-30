using System.Collections.Generic;
using JetBrains.Annotations;

namespace ProbabilisticEngine.Data
{
    [System.Serializable]
    public class ItemDefinition
    {
        public string Id;
        public float BaseWeight;
        
        [CanBeNull] public List<ConditionDefinition> Conditions;
        [CanBeNull] public List<OptionDefinition> Options;
    }
}