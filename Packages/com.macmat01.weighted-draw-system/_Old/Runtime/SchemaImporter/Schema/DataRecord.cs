using System;
using System.Collections.Generic;
namespace _Old.Runtime.SchemaImporter.Schema
{
    /// <summary>
    ///     Represents a single parsed row from a CSV file.
    ///     The Fields dictionary maps column names to their parsed values.
    ///     For ConditionList columns, the value is a List<ParsedCondition>.
    /// </summary>
    [Obsolete("Legacy _Old API. Use the new MacMat01.WeightedDrawSystem package APIs.", false)]
    public class DataRecord
    {
        private readonly Dictionary<string, object> fields = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, object> Fields => fields;

        public void SetField(string columnName, object value)
        {
            fields[columnName] = value;
        }

        public object GetField(string columnName)
        {
            return fields.GetValueOrDefault(columnName);

        }

        public bool TryGetField(string columnName, out object value)
        {
            return fields.TryGetValue(columnName, out value);

        }

        public override string ToString()
        {
            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, object> kvp in fields)
            {
                parts.Add($"{kvp.Key}={kvp.Value}");
            }

            return "{" + string.Join(", ", parts) + "}";
        }
    }
}
