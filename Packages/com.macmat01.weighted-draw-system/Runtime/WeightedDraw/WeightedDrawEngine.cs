using System;
using System.Collections.Generic;
using System.Linq;
namespace WeightedDraw
{
    public sealed class WeightedDrawEngine<TEntry, TContext>
    {
        private readonly Func<TEntry, TContext, bool> isEligible;
        private readonly IRandomValueProvider randomValueProvider;
        private readonly Func<TEntry, float> weightSelector;

        public WeightedDrawEngine(
            Func<TEntry, TContext, bool> isEligible,
            Func<TEntry, float> weightSelector,
            IRandomValueProvider randomValueProvider = null)
        {
            this.isEligible = isEligible ?? throw new ArgumentNullException(nameof(isEligible));
            this.weightSelector = weightSelector ?? throw new ArgumentNullException(nameof(weightSelector));
            this.randomValueProvider = randomValueProvider ?? UnityRandomValueProvider.Shared;
        }

        public IReadOnlyList<TEntry> GetValidEntries(IEnumerable<TEntry> source, TContext context)
        {
            List<TEntry> validEntries = new List<TEntry>();
            if (source == null)
            {
                return validEntries;
            }

            validEntries.AddRange(source.Where(entry => isEligible(entry, context)));

            return validEntries;
        }

        public TEntry Draw(IEnumerable<TEntry> source, TContext context)
        {
            List<TEntry> validEntries = new List<TEntry>(GetValidEntries(source, context));
            if (validEntries.Count == 0)
            {
                return default;
            }

            float totalWeight = validEntries.Sum(t => Math.Max(0f, weightSelector(t)));

            if (totalWeight <= 0f)
            {
                int uniformIndex = randomValueProvider.NextInt(0, validEntries.Count);
                return validEntries[uniformIndex];
            }

            float target = randomValueProvider.NextFloat(0f, totalWeight);
            float cumulative = 0f;
            foreach (TEntry t in validEntries)
            {
                cumulative += Math.Max(0f, weightSelector(t));
                if (target <= cumulative)
                {
                    return t;
                }
            }

            return validEntries[^1];
        }
    }
}
