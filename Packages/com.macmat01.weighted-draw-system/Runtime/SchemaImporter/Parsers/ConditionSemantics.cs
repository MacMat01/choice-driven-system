using System;
using System.Globalization;
using UnityEngine;

namespace SchemaImporter.Parsers
{
    /// <summary>
    ///     Shared condition semantics used by both the importer and runtime evaluator.
    /// </summary>
    public static class ConditionSemantics
    {
        public const string AndConnector = "AND";
        public const string OrConnector = "OR";

        public static readonly string[] SupportedOperators =
        {
            "==",
            "!=",
            ">=",
            "<=",
            ">",
            "<"
        };

        public static bool TryParseOperatorCondition(string conditionPart, out string variableName, out string op, out float value)
        {
            variableName = null;
            op = null;
            value = 0f;

            if (string.IsNullOrWhiteSpace(conditionPart))
            {
                return false;
            }

            foreach (string supportedOperator in SupportedOperators)
            {
                int opIndex = conditionPart.IndexOf(supportedOperator, StringComparison.Ordinal);
                if (opIndex <= 0)
                {
                    continue;
                }

                string varName = conditionPart[..opIndex].Trim();
                string valueStr = conditionPart[(opIndex + supportedOperator.Length)..].Trim();
                if (!string.IsNullOrEmpty(varName) && TryParseFloatInvariant(valueStr, out value))
                {
                    variableName = varName;
                    op = supportedOperator;
                    return true;
                }
            }

            return false;
        }

        public static string NormalizeConnector(string rawConnector)
        {
            if (string.IsNullOrWhiteSpace(rawConnector))
            {
                return null;
            }

            string token = rawConnector.Trim().ToLowerInvariant();
            return token switch
            {
                "&&" or "&" or "and" or ";" => AndConnector,
                "||" or "|" or "or" => OrConnector,
                _ => null
            };
        }

        public static bool TryParseFloatInvariant(string value, out float parsed)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
        }

        public static bool TryConvertToFloat(object rawValue, out float value)
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

        public static bool EvaluateComparison(float leftValue, string op, float rightValue)
        {
            return op switch
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
    }
}


