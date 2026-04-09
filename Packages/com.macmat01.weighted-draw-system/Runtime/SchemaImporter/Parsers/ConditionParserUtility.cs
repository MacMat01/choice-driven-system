using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
namespace SchemaImporter.Parsers
{
    /// <summary>
    ///     Parses condition strings into structured ParsedCondition objects.
    ///     Supports operators: ==, !=, &gt;=, &lt;=, &gt;, &lt;.
    ///     Supports connectors as separators: &amp;&amp;, ||, and/or, &, |, ;.
    ///     Also supports boolean flags like "alchemy" (== 1) or "!alchemy" (!= 1).
    ///     Also supports literal boolean constants: TRUE (always true), FALSE (always false).
    /// </summary>
    public static class ConditionParserUtility
    {
        private static readonly Regex ConnectorRegex = new Regex(@"&&|\|\||\band\b|\bor\b|&|\||;", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
            MatchCollection connectorMatches = ConnectorRegex.Matches(trimmed);

            int segmentStart = 0;
            string pendingConnector = null;
            string pendingRawConnector = null;

            foreach (Match connectorMatch in connectorMatches)
            {
                string segment = trimmed.Substring(segmentStart, connectorMatch.Index - segmentStart).Trim();
                TryParseAndAdd(conditions, segment, pendingConnector, pendingRawConnector);

                pendingRawConnector = connectorMatch.Value.Trim();
                pendingConnector = NormalizeConnector(pendingRawConnector);
                segmentStart = connectorMatch.Index + connectorMatch.Length;
            }

            string finalSegment = trimmed[segmentStart..].Trim();
            TryParseAndAdd(conditions, finalSegment, pendingConnector, pendingRawConnector);

            return conditions;
        }

        private static ParsedCondition ParseSingleCondition(string conditionPart)
        {
            conditionPart = conditionPart.Trim();

            // Try to parse as a literal boolean constant first (e.g., "TRUE" or "FALSE").
            if (TryParseAsLiteralBoolean(conditionPart, out ParsedCondition booleanConstant))
            {
                return booleanConstant;
            }

            // Try to parse as a boolean flag (e.g., "alchemy" or "!alchemy").
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

        private static void TryParseAndAdd(
            List<ParsedCondition> conditions,
            string segment,
            string connectorFromPrevious,
            string rawConnectorFromPrevious)
        {
            if (string.IsNullOrEmpty(segment))
            {
                return;
            }

            ParsedCondition condition = ParseSingleCondition(segment);
            if (condition == null)
            {
                return;
            }

            condition.ConnectorFromPrevious = conditions.Count == 0 ? null : connectorFromPrevious;
            condition.RawConnectorFromPrevious = conditions.Count == 0 ? null : rawConnectorFromPrevious;
            conditions.Add(condition);
        }

        private static string NormalizeConnector(string rawConnector)
        {
            if (string.IsNullOrWhiteSpace(rawConnector))
            {
                return null;
            }

            string token = rawConnector.Trim().ToLowerInvariant();
            return token switch
            {
                "&&" or "&" or "and" or ";" => "AND",
                "||" or "|" or "or" => "OR",
                _ => null
            };
        }

        private static bool TryParseAsLiteralBoolean(string conditionPart, out ParsedCondition booleanConstant)
        {
            booleanConstant = null;

            string upperPart = conditionPart.ToUpperInvariant();

            switch (upperPart)
            {
                case "TRUE":
                    booleanConstant = ParsedCondition.CreateBooleanLiteral(true);
                    return true;
                case "FALSE":
                    booleanConstant = ParsedCondition.CreateBooleanLiteral(false);
                    return true;
                default:
                    return false;
            }

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
