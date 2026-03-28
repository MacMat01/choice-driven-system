using System;
namespace Core.DynamicData
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

        public ParsedCondition(string variableName, string op, float value)
        {
            VariableName = variableName;
            Operator = op;
            Value = value;
        }

        public override string ToString()
        {
            return $"{VariableName} {Operator} {Value}";
        }
    }
}
