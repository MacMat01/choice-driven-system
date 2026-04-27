using System;
using System.Collections.Generic;
using _Old.Runtime.SchemaImporter.Parsers;
namespace _Old.Runtime.ProbabilityEngine.Core
{
    static class ParsedConditionEvaluator
    {
        public static bool Evaluate(ParsedCondition condition, IReadOnlyDictionary<string, object> gameStateContext)
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

            return ConditionSemantics.EvaluateComparison(leftValue, condition.Operator, condition.Value);
        }

        public static bool EvaluateChain(IReadOnlyList<ParsedCondition> conditions, IReadOnlyDictionary<string, object> gameStateContext)
        {
            if (conditions == null || conditions.Count == 0)
            {
                return true;
            }

            bool aggregate = Evaluate(conditions[0], gameStateContext);
            for (int i = 1; i < conditions.Count; i++)
            {
                ParsedCondition condition = conditions[i];
                bool current = Evaluate(condition, gameStateContext);
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

                return ConditionSemantics.TryConvertToFloat(pair.Value, out value);
            }

            return false;
        }
    }
}
