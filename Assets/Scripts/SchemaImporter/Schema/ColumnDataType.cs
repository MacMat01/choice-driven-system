namespace SchemaImporter.Schema
{
    /// <summary>
    ///     Defines the type of data expected in a CSV column.
    /// </summary>
    public enum ColumnDataType
    {
        String,
        Int,
        Float,
        Bool,
        ConditionList,
        WeightColumn
    }
}
