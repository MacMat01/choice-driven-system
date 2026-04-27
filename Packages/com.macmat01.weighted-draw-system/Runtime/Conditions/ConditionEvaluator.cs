using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
namespace Conditions
{
    public interface IConditionEvaluator
    {
        bool Evaluate(string expression, IGameStateReader gameState);
    }

    public sealed class ConditionEvaluator : IConditionEvaluator
    {
        private static readonly string[] SupportedOperators =
        {
            "==",
            "!=",
            ">=",
            "<=",
            ">",
            "<"
        };

        public bool Evaluate(string expression, IGameStateReader gameState)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return true;
            }

            if (gameState == null)
            {
                return false;
            }

            List<string> segments = new List<string>();
            List<string> connectors = new List<string>();
            SplitExpression(expression, segments, connectors);
            if (segments.Count == 0)
            {
                return true;
            }

            bool aggregate = EvaluateSingle(segments[0], gameState);
            for (int i = 1; i < segments.Count; i++)
            {
                bool current = EvaluateSingle(segments[i], gameState);
                string connector = connectors[i - 1];
                aggregate = connector == "OR" ? aggregate || current : aggregate && current;
            }

            return aggregate;
        }

        private static void SplitExpression(string expression, ICollection<string> segments, ICollection<string> connectors)
        {
            int start = 0;
            int index = 0;
            while (index < expression.Length)
            {
                if (TryReadConnector(expression, index, out string connector, out int connectorLength))
                {
                    string segment = expression.Substring(start, index - start).Trim();
                    if (!string.IsNullOrEmpty(segment))
                    {
                        segments.Add(segment);
                        connectors.Add(connector);
                    }

                    index += connectorLength;
                    start = index;
                    continue;
                }

                index++;
            }

            string finalSegment = expression[start..].Trim();
            if (!string.IsNullOrEmpty(finalSegment))
            {
                segments.Add(finalSegment);
            }

            while (connectors.Count >= segments.Count)
            {
                // Keep connectors aligned with segmentCount - 1 when malformed input is provided.
                if (connectors is List<string> connectorList)
                {
                    connectorList.RemoveAt(connectorList.Count - 1);
                }
                else
                {
                    break;
                }
            }
        }

        private static bool TryReadConnector(string expression, int index, out string connector, out int connectorLength)
        {
            connector = null;
            connectorLength = 0;

            if (index + 1 < expression.Length)
            {
                string pair = expression.Substring(index, 2);
                switch (pair)
                {
                    case "&&":
                        connector = "AND";
                        connectorLength = 2;
                        return true;
                    case "||":
                        connector = "OR";
                        connectorLength = 2;
                        return true;
                }

            }

            switch (expression[index])
            {
                case '&':
                case ';':
                    connector = "AND";
                    connectorLength = 1;
                    return true;
                case '|':
                    connector = "OR";
                    connectorLength = 1;
                    return true;
            }

            if (TryReadWord(expression, index, "and", out connectorLength))
            {
                connector = "AND";
                return true;
            }

            if (TryReadWord(expression, index, "or", out connectorLength))
            {
                connector = "OR";
                return true;
            }

            return false;
        }

        private static bool TryReadWord(string expression, int index, string word, out int length)
        {
            length = word.Length;
            if (index + length > expression.Length)
            {
                return false;
            }

            string token = expression.Substring(index, length);
            if (!string.Equals(token, word, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            bool hasLeftBoundary = index == 0 || !char.IsLetterOrDigit(expression[index - 1]);
            bool hasRightBoundary = index + length >= expression.Length || !char.IsLetterOrDigit(expression[index + length]);
            return hasLeftBoundary && hasRightBoundary;
        }

        private static bool EvaluateSingle(string segment, IGameStateReader gameState)
        {
            string trimmed = segment.Trim();
            if (string.Equals(trimmed, "TRUE", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(trimmed, "FALSE", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (trimmed.StartsWith("!", StringComparison.Ordinal))
            {
                return !EvaluateFlag(trimmed[1..], gameState);
            }

            foreach (string op in SupportedOperators)
            {
                int opIndex = trimmed.IndexOf(op, StringComparison.Ordinal);
                if (opIndex <= 0)
                {
                    continue;
                }

                string key = trimmed[..opIndex].Trim();
                string rightPart = trimmed[(opIndex + op.Length)..].Trim();
                if (!float.TryParse(rightPart, NumberStyles.Float, CultureInfo.InvariantCulture, out float rightValue))
                {
                    return false;
                }

                if (!gameState.TryGetValue(key, out float leftValue))
                {
                    return false;
                }

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

            return EvaluateFlag(trimmed, gameState);
        }

        private static bool EvaluateFlag(string key, IGameStateReader gameState)
        {
            return gameState.TryGetValue(key.Trim(), out float value) && !Mathf.Approximately(value, 0f);
        }
    }
}
