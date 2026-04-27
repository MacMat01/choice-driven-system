using System;
namespace _Old.Runtime.SchemaImporter.Parsers
{
    /// <summary>
    ///     Represents a single parsed condition from a condition string.
    ///     Example: "army > 40" parses to:
    ///     VariableName: "army"
    ///     Operator: ">"
    ///     Value: 40
    /// </summary>
    [Serializable]
    [Obsolete("Legacy _Old API. Use the new MacMat01.WeightedDrawSystem package APIs.", false)]
    public class ParsedCondition
    {
        public string VariableName;
        public string Operator;
        public float Value;
        // True when this condition is a literal boolean constant (TRUE/FALSE).
        public bool IsBooleanLiteral;
        // Literal value when IsBooleanLiteral is true.
        public bool BooleanLiteralValue;
        // Canonical connector from the previous condition: "AND" or "OR". Null for first condition.
        public string ConnectorFromPrevious;
        // Original connector token as it appeared in input (e.g. "&&", "or", ";").
        public string RawConnectorFromPrevious;

        public ParsedCondition(
            string variableName,
            string op,
            float value,
            string connectorFromPrevious = null,
            string rawConnectorFromPrevious = null,
            bool isBooleanLiteral = false,
            bool booleanLiteralValue = false)
        {
            VariableName = variableName;
            Operator = op;
            Value = value;
            IsBooleanLiteral = isBooleanLiteral;
            BooleanLiteralValue = booleanLiteralValue;
            ConnectorFromPrevious = connectorFromPrevious;
            RawConnectorFromPrevious = rawConnectorFromPrevious;
        }

        public static ParsedCondition CreateBooleanLiteral(bool literalValue)
        {
            return new ParsedCondition(
                string.Empty,
                "==",
                literalValue ? 1f : 0f,
                isBooleanLiteral: true,
                booleanLiteralValue: literalValue);
        }

        public override string ToString()
        {
            if (IsBooleanLiteral)
            {
                return BooleanLiteralValue ? "TRUE" : "FALSE";
            }

            return $"{VariableName} {Operator} {Value}";
        }
    }
}
