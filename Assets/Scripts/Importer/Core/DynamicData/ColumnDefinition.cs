using System;
namespace Importer.Core.DynamicData
{
    /// <summary>
    ///     Defines a single column in a CSV schema.
    ///     The ColumnName must match the CSV header exactly (case-insensitive matching recommended).
    /// </summary>
    [Serializable]
    public class ColumnDefinition
    {
        public string ColumnName;
        public ColumnDataType DataType;

        public ColumnDefinition(string columnName, ColumnDataType dataType)
        {
            ColumnName = columnName;
            DataType = dataType;
        }
    }
}
