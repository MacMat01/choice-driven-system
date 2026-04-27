using System;
using System.Collections.Generic;
using System.Linq;
using _Old.Runtime.ProbabilityEngine.Interfaces;
using _Old.Runtime.SchemaImporter.Parsers;
using _Old.Runtime.SchemaImporter.Schema;
namespace _Old.Runtime.ProbabilityEngine.Core
{
    /// <summary>
    ///     Filters imported DataRecord rows by ParsedCondition lists and selects one valid row at random.
    /// </summary>
    [Obsolete("Legacy _Old API. Use the new MacMat01.WeightedDrawSystem package APIs.", false)]
    public sealed class RandomiserSystem
    {
        private readonly string conditionColumnName;
        private readonly ProbabilityEngine<DictionaryGameState, DataRecord> probabilityEngine;
        private readonly string weightColumnName;

        private RandomiserSystem(
            IEnumerable<DataRecord> items,
            string conditionColumnName,
            string weightColumnName = null,
            IRandomValueProvider randomValueProvider = null)
        {
            this.conditionColumnName = conditionColumnName;
            this.weightColumnName = weightColumnName;
            probabilityEngine = new ProbabilityEngine<DictionaryGameState, DataRecord>(CreateProbabilityItems(items), randomValueProvider);
        }

        public RandomiserSystem(
            IEnumerable<DataRecord> items,
            DataSchemaSO schema,
            string conditionColumnName = null,
            string weightColumnName = null)
            : this(items, schema, null, conditionColumnName, weightColumnName)
        {
        }

        public RandomiserSystem(
            IEnumerable<DataRecord> items,
            DataSchemaSO schema,
            IRandomValueProvider randomValueProvider,
            string conditionColumnName = null,
            string weightColumnName = null)
            : this(
                items,
                string.IsNullOrWhiteSpace(conditionColumnName)
                    ? ResolveColumnByType(schema, ColumnDataType.ConditionList)
                    : conditionColumnName,
                string.IsNullOrWhiteSpace(weightColumnName)
                    ? ResolveColumnByType(schema, ColumnDataType.WeightColumn)
                    : weightColumnName,
                randomValueProvider)
        {
        }

        /// <summary>
        ///     Returns only rows whose parsed conditions evaluate to true for the provided context.
        /// </summary>
        public List<DataRecord> GetValidChoices(IReadOnlyDictionary<string, object> gameStateContext)
        {
            DictionaryGameState state = new DictionaryGameState(gameStateContext);
            return probabilityEngine.GetValidChoices(state)
                .Select(static item => item.Value)
                .Where(static item => item != null)
                .ToList();
        }

        /// <summary>
        ///     Selects one valid row based on weighted probability ratios.
        /// </summary>
        public DataRecord EvaluateRandom(IReadOnlyDictionary<string, object> gameStateContext)
        {
            DictionaryGameState state = new DictionaryGameState(gameStateContext);
            ProbabilityItem<DictionaryGameState, DataRecord> selected = probabilityEngine.EvaluateRandom(state);
            return selected?.Value;
        }

        private IEnumerable<ProbabilityItem<DictionaryGameState, DataRecord>> CreateProbabilityItems(IEnumerable<DataRecord> sourceItems)
        {
            if (sourceItems == null)
            {
                yield break;
            }

            foreach (DataRecord item in sourceItems)
            {
                if (item == null)
                {
                    continue;
                }

                yield return new ProbabilityItem<DictionaryGameState, DataRecord>
                {
                    Id = ResolveRecordId(item),
                    Value = item,
                    BaseWeight = ResolveWeight(item),
                    Conditions = BuildConditions(item)
                };
            }
        }

        private static string ResolveRecordId(DataRecord item)
        {
            if (item == null)
            {
                return null;
            }

            if (item.TryGetField("Card_ID", out object cardId) && cardId != null)
            {
                return cardId.ToString();
            }

            if (item.TryGetField("Id", out object idValue) && idValue != null)
            {
                return idValue.ToString();
            }

            return null;
        }

        private float ResolveWeight(DataRecord item)
        {
            if (string.IsNullOrWhiteSpace(weightColumnName))
            {
                return 1f;
            }

            object rawWeight = item.GetField(weightColumnName);
            if (ConditionSemantics.TryConvertToFloat(rawWeight, out float parsedWeight) && parsedWeight > 0f)
            {
                return parsedWeight;
            }

            return 0f;
        }

        private List<ICondition<DictionaryGameState>> BuildConditions(DataRecord item)
        {
            if (string.IsNullOrWhiteSpace(conditionColumnName))
            {
                return null;
            }

            object rawConditionList = item.GetField(conditionColumnName);
            if (rawConditionList == null)
            {
                return null;
            }

            if (rawConditionList is not List<ParsedCondition> parsedConditions)
            {
                return new List<ICondition<DictionaryGameState>>
                {
                    new AlwaysFalseCondition()
                };
            }

            if (parsedConditions.Count == 0)
            {
                return null;
            }

            return new List<ICondition<DictionaryGameState>>
            {
                new ParsedConditionChainCondition(parsedConditions)
            };
        }

        private static string ResolveColumnByType(DataSchemaSO schema, ColumnDataType type)
        {
            if (schema?.Columns == null)
            {
                return null;
            }

            foreach (ColumnDefinition column in schema.Columns)
            {
                if (column != null && column.DataType == type && !string.IsNullOrWhiteSpace(column.ColumnName))
                {
                    return column.ColumnName;
                }
            }

            return null;
        }

        private sealed class DictionaryGameState : IGameState
        {
            public DictionaryGameState(IReadOnlyDictionary<string, object> values)
            {
                Values = values;
            }

            public IReadOnlyDictionary<string, object> Values { get; }
        }

        private sealed class AlwaysFalseCondition : ICondition<DictionaryGameState>
        {
            public bool Evaluate(DictionaryGameState state)
            {
                return false;
            }
        }

        private sealed class ParsedConditionChainCondition : ICondition<DictionaryGameState>
        {
            private readonly IReadOnlyList<ParsedCondition> conditions;

            public ParsedConditionChainCondition(IReadOnlyList<ParsedCondition> conditions)
            {
                this.conditions = conditions;
            }

            public bool Evaluate(DictionaryGameState state)
            {
                return ParsedConditionEvaluator.EvaluateChain(conditions, state?.Values);
            }
        }
    }
}
