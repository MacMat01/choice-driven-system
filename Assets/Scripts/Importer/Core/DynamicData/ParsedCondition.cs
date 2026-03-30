using System;
namespace Importer.Core.DynamicData
{
    /// <summary>
    ///     Represents a single parsed condition from a condition string.
    ///     Example: "army > 40" parses to:
    ///     VariableName: "army"
    ///     Operator: ">"
    ///     Value: 40
    /// </summary>
    [Serializable]
    public class ParsedCondition
    {
        public string VariableName;
        public string Operator;
        public float Value;
        // Canonical connector from the previous condition: "AND" or "OR". Null for first condition.
        public string ConnectorFromPrevious;
        // Original connector token as it appeared in input (e.g. "&&", "or", ";").
        public string RawConnectorFromPrevious;

        public ParsedCondition(
            string variableName,
            string op,
            float value,
            string connectorFromPrevious = null,
            string rawConnectorFromPrevious = null)
        {
            VariableName = variableName;
            Operator = op;
            Value = value;
            ConnectorFromPrevious = connectorFromPrevious;
            RawConnectorFromPrevious = rawConnectorFromPrevious;
        }

        public override string ToString()
        {
            return $"{VariableName} {Operator} {Value}";
        }
    }
}
