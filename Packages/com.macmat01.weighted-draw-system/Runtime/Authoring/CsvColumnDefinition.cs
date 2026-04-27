using System;
namespace Authoring
{
    [Serializable]
    public sealed class CsvColumnDefinition
    {
        public string ColumnName;
        public bool IsRequired;

        public CsvColumnDefinition()
        {
        }

        public CsvColumnDefinition(string columnName, bool isRequired = false)
        {
            ColumnName = columnName;
            IsRequired = isRequired;
        }
    }
}
