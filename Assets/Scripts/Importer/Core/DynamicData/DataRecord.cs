using System.Collections.Generic;
namespace Importer.Core.DynamicData
{
    /// <summary>
    ///     Represents a single parsed row from a CSV file.
    ///     The Fields dictionary maps column names to their parsed values.
    ///     For ConditionList columns, the value is a List<ParsedCondition>.
    /// </summary>
    public class DataRecord
    {
        private Dictionary<string, object> Fields
        {
            get;
        } = new Dictionary<string, object>();

        public void SetField(string columnName, object value)
        {
            Fields[columnName] = value;
        }

        public object GetField(string columnName)
        {
            return Fields.GetValueOrDefault(columnName);

        }

        public override string ToString()
        {
            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, object> kvp in Fields)
            {
                parts.Add($"{kvp.Key}={kvp.Value}");
            }

            return "{" + string.Join(", ", parts) + "}";
        }
    }
}
