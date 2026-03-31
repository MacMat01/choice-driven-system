using System;
using System.Collections.Generic;
using UnityEngine;
namespace SchemaImporter.Schema
{
    /// <summary>
    ///     ScriptableObject that defines the schema for data import.
    ///     Designers define expected columns and must also assign a CSV/JSON source file.
    /// </summary>
    [CreateAssetMenu(fileName = "DataSchema", menuName = "SchemaImporter/Data Schema")]
    // ReSharper disable once InconsistentNaming
    public class DataSchemaSO : ScriptableObject
    {
        [Header("Schema Definition")]
        [Tooltip("List of expected columns and data types. Column names must match CSV/JSON keys.")]
        [SerializeField]
        // ReSharper disable once InconsistentNaming
        private List<ColumnDefinition> columns = new List<ColumnDefinition>();

        [Header("Source File")]
        [Tooltip("CSV or JSON TextAsset used as the import source for this schema.")]
        [SerializeField]
        // ReSharper disable once InconsistentNaming
        private TextAsset sourceDataFile;

        public List<ColumnDefinition> Columns => columns;
        public TextAsset SourceDataFile => sourceDataFile;

        public ColumnDefinition GetColumn(string columnName)
        {
            foreach (ColumnDefinition column in Columns)
            {
                if (string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return column;
                }
            }

            return null;
        }

        public bool HasSourceDataFile()
        {
            return sourceDataFile != null;
        }
    }
}
