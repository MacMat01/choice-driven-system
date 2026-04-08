using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
namespace SchemaImporter.Schema
{
    /// <summary>
    ///     ScriptableObject that defines the schema for data import.
    ///     Designers define expected columns and must also assign a CSV/JSON source file.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [CreateAssetMenu(fileName = "DataSchema", menuName = "SchemaImporter/Data Schema")]
    public class DataSchemaSO : ScriptableObject
    {
        [Header("Schema Definition")]
        [Tooltip("List of expected columns and data types. Column names must match CSV/JSON keys.")]
        [SerializeField]
        private List<ColumnDefinition> columns = new List<ColumnDefinition>();

        [Header("Source File")]
        [Tooltip("CSV or JSON TextAsset used as the import source for this schema.")]
        [SerializeField]
        private TextAsset sourceDataFile;

        public List<ColumnDefinition> Columns => columns;
        public TextAsset SourceDataFile => sourceDataFile;

        public bool HasSourceDataFile()
        {
            return sourceDataFile != null;
        }
    }
}
