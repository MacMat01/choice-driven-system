using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
namespace _Old.Runtime.SchemaImporter.Schema
{
    /// <summary>
    ///     ScriptableObject that defines the schema for data import.
    ///     Designers define expected columns and must also assign a CSV/JSON source file.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [CreateAssetMenu(fileName = "DataSchema", menuName = "SchemaImporter/Data Schema")]
    [Obsolete("Legacy _Old API. Use the new MacMat01.WeightedDrawSystem package APIs.", false)]
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

        [SerializeField] [HideInInspector]
        private string lastImportedFileName;

        public List<ColumnDefinition> Columns => columns;
        public TextAsset SourceDataFile => sourceDataFile;

        private void OnValidate()
        {
            if (sourceDataFile == null)
            {
                ResetDataSource();
                return;
            }

            if (lastImportedFileName == sourceDataFile.name)
            {
                return;
            }

            lastImportedFileName = sourceDataFile.name;
            GenerateColumnsFromCSV();
        }

        public bool HasSourceDataFile()
        {
            return sourceDataFile != null;
        }

        private void GenerateColumnsFromCSV()
        {
            string text = sourceDataFile.text;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            string firstLine = text.Split('\n')[0];
            string[] headers = firstLine.Split(',');

            columns = headers
                .Select(h => new ColumnDefinition(h.Trim()))
                .ToList();
        }

        private void ResetDataSource()
        {
            lastImportedFileName = null;
            columns.Clear();
        }
    }
}
