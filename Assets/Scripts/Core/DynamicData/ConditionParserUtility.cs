using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
namespace Core.DynamicData
{
    /// <summary>
    ///     Parses condition strings into structured ParsedCondition objects.
    ///     Supports operators: ==, !=, &gt;=, &lt;=, &gt;, &lt;.
    ///     Supports connectors as separators: &amp;&amp;, ||, and/or, &, |, ;.
    ///     Also supports boolean flags like "alchemy" (== 1) or "!alchemy" (!= 1).
    /// </summary>
    public static class ConditionParserUtility
    {
        private static readonly Regex ConnectorSplitRegex = new Regex(@"\s*(?:&&|\|\||\band\b|\bor\b|&|\||;)\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly string[] SupportedOperators =
        {
            "==",
            "!=",
            ">=",
            "<=",
            ">",
            "<"
        };

        /// <summary>
        ///     Parses a condition string into a list of ParsedCondition objects.
        ///     Example: "army&gt;40&amp;&amp;gold&lt;10" -&gt; [ ParsedCondition("army", ">", 40), ParsedCondition("gold", "<", 10) ]
        /// </summary>
        public static List<ParsedCondition> Parse(string rawConditionString)
        {
            List<ParsedCondition> conditions = new List<ParsedCondition>();

            if (string.IsNullOrWhiteSpace(rawConditionString))
            {
                return conditions;
            }

            string trimmed = rawConditionString.Trim();
            string[] parts = ConnectorSplitRegex.Split(trimmed);

            foreach (string part in parts)
            {
                string cleanPart = part.Trim();
                if (string.IsNullOrEmpty(cleanPart))
                {
                    continue;
                }

                ParsedCondition condition = ParseSingleCondition(cleanPart);
                if (condition != null)
                {
                    conditions.Add(condition);
                }
            }

            return conditions;
        }

        private static ParsedCondition ParseSingleCondition(string conditionPart)
        {
            conditionPart = conditionPart.Trim();

            // Try to parse as a boolean flag first (e.g., "alchemy" or "!alchemy").
            if (TryParseAsFlag(conditionPart, out ParsedCondition flagCondition))
            {
                return flagCondition;
            }

            // Try to parse as an operator-based condition (e.g., "army>40").
            foreach (string op in SupportedOperators)
            {
                int opIndex = conditionPart.IndexOf(op, StringComparison.Ordinal);
                if (opIndex > 0)
                {
                    string varName = conditionPart[..opIndex].Trim();
                    string valueStr = conditionPart[(opIndex + op.Length)..].Trim();

                    if (!string.IsNullOrEmpty(varName) && float.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                    {
                        return new ParsedCondition(varName, op, value);
                    }
                }
            }

            return null;
        }

        private static bool TryParseAsFlag(string conditionPart, out ParsedCondition flagCondition)
        {
            flagCondition = null;

            if (conditionPart.StartsWith("!", StringComparison.Ordinal))
            {
                string varName = conditionPart[1..].Trim();
                if (!string.IsNullOrEmpty(varName) && IsValidVariableName(varName))
                {
                    flagCondition = new ParsedCondition(varName, "!=", 1);
                    return true;
                }
            }
            else if (IsValidVariableName(conditionPart))
            {
                flagCondition = new ParsedCondition(conditionPart, "==", 1);
                return true;
            }

            return false;
        }

        private static bool IsValidVariableName(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                // Simple validation: alphanumeric and underscores only.
                Regex.IsMatch(name, "^[a-zA-Z_][a-zA-Z0-9_]*$");

        }
    }
}
