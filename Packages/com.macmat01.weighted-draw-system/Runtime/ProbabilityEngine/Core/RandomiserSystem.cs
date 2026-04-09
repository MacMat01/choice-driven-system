using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ProbabilityEngine.Interfaces;
using SchemaImporter.Parsers;
using SchemaImporter.Schema;
using UnityEngine;
namespace ProbabilityEngine.Core
{
    /// <summary>
    ///     Filters imported DataRecord rows by ParsedCondition lists and selects one valid row at random.
    /// </summary>
    public sealed class RandomiserSystem
    {
        private readonly string conditionColumnName;
        private readonly ProbabilityEngine<DictionaryGameState, DataRecord> probabilityEngine;
        private readonly string weightColumnName;

        private RandomiserSystem(
            IEnumerable<DataRecord> items,
            string conditionColumnName,
            string weightColumnName = null)
        {
            this.conditionColumnName = conditionColumnName;
            this.weightColumnName = weightColumnName;
            probabilityEngine = new ProbabilityEngine<DictionaryGameState, DataRecord>(CreateProbabilityItems(items));
        }

        public RandomiserSystem(
            IEnumerable<DataRecord> items,
            DataSchemaSO schema,
            string conditionColumnName = null,
            string weightColumnName = null)
            : this(
                items,
                string.IsNullOrWhiteSpace(conditionColumnName)
                    ? ResolveColumnByType(schema, ColumnDataType.ConditionList)
                    : conditionColumnName,
                string.IsNullOrWhiteSpace(weightColumnName)
                    ? ResolveColumnByType(schema, ColumnDataType.WeightColumn)
                    : weightColumnName)
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
                    Id = item.GetField("Card_ID")?.ToString(),
                    Value = item,
                    BaseWeight = ResolveWeight(item),
                    Conditions = BuildConditions(item)
                };
            }
        }

        private float ResolveWeight(DataRecord item)
        {
            if (string.IsNullOrWhiteSpace(weightColumnName))
            {
                return 1f;
            }

            object rawWeight = item.GetField(weightColumnName);
            if (TryConvertToFloat(rawWeight, out float parsedWeight) && parsedWeight > 0f)
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

        private static bool EvaluateCondition(ParsedCondition condition, IReadOnlyDictionary<string, object> gameStateContext)
        {
            if (condition == null)
            {
                return false;
            }

            if (condition.IsBooleanLiteral)
            {
                return condition.BooleanLiteralValue;
            }

            if (!TryResolveContextValue(gameStateContext, condition.VariableName, out float leftValue))
            {
                return false;
            }

            float rightValue = condition.Value;
            return condition.Operator switch
            {
                "==" => Mathf.Approximately(leftValue, rightValue),
                "!=" => !Mathf.Approximately(leftValue, rightValue),
                ">" => leftValue > rightValue,
                "<" => leftValue < rightValue,
                ">=" => leftValue >= rightValue,
                "<=" => leftValue <= rightValue,
                _ => false
            };
        }

        private static bool TryResolveContextValue(IReadOnlyDictionary<string, object> gameStateContext, string variableName, out float value)
        {
            value = 0f;
            if (string.IsNullOrWhiteSpace(variableName) || gameStateContext == null || gameStateContext.Count == 0)
            {
                return false;
            }

            foreach (KeyValuePair<string, object> pair in gameStateContext)
            {
                if (!string.Equals(pair.Key, variableName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return TryConvertToFloat(pair.Value, out value);
            }

            return false;
        }

        private static bool TryConvertToFloat(object rawValue, out float value)
        {
            value = 0f;
            switch (rawValue)
            {
                case null:
                    return false;
                case float floatValue:
                    value = floatValue;
                    return true;
                case double doubleValue:
                    value = (float)doubleValue;
                    return true;
                case int intValue:
                    value = intValue;
                    return true;
                case long longValue:
                    value = longValue;
                    return true;
                case bool boolValue:
                    value = boolValue ? 1f : 0f;
                    return true;
                case string stringValue:
                    if (float.TryParse(stringValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float parsedStringFloat))
                    {
                        value = parsedStringFloat;
                        return true;
                    }

                    if (bool.TryParse(stringValue, out bool parsedStringBool))
                    {
                        value = parsedStringBool ? 1f : 0f;
                        return true;
                    }

                    return false;
                default:
                    try
                    {
                        value = Convert.ToSingle(rawValue, CultureInfo.InvariantCulture);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
            }
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
                if (conditions == null || conditions.Count == 0)
                {
                    return true;
                }

                bool aggregate = EvaluateCondition(conditions[0], state?.Values);
                for (int i = 1; i < conditions.Count; i++)
                {
                    ParsedCondition condition = conditions[i];
                    bool current = EvaluateCondition(condition, state?.Values);
                    if (string.Equals(condition.ConnectorFromPrevious, "OR", StringComparison.OrdinalIgnoreCase))
                    {
                        aggregate = aggregate || current;
                    }
                    else
                    {
                        aggregate = aggregate && current;
                    }
                }

                return aggregate;
            }
        }
    }
}
