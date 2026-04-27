using System;
namespace _Old.Runtime.SchemaImporter.Schema
{
    /// <summary>
    ///     Defines a single column in a CSV schema.
    ///     The ColumnName must match the CSV header exactly (case-insensitive matching recommended).
    ///     IsRequired controls whether empty cells are allowed (false = optional, true = required).
    /// </summary>
    [Serializable]
    [Obsolete("Legacy _Old API. Use the new MacMat01.WeightedDrawSystem package APIs.", false)]
    public class ColumnDefinition
    {
        public string ColumnName;
        public ColumnDataType DataType;
        public bool IsRequired;

        public ColumnDefinition(string columnName)
        {
            ColumnName = columnName;
        }

        public ColumnDefinition(string columnName, ColumnDataType dataType, bool isRequired = false)
        {
            ColumnName = columnName;
            DataType = dataType;
            IsRequired = isRequired;
        }
    }
}
