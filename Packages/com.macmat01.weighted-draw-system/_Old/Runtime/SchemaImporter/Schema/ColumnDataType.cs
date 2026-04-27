using System;
namespace _Old.Runtime.SchemaImporter.Schema
{
    /// <summary>
    ///     Defines the type of data expected in a CSV column.
    /// </summary>
    [Obsolete("Legacy _Old API. Use the new MacMat01.WeightedDrawSystem package APIs.", false)]
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
