using System;
using System.Collections.Generic;
using Csv;
using UnityEngine;
namespace Authoring
{
    /// <summary>
    ///     Generic authoring ScriptableObject for CSV-to-object import.
    ///     Subclass this and override GetDeserializer() to provide your custom row mapping.
    /// </summary>
    public abstract class CsvDataSourceSO<T> : ScriptableObject where T : class
    {
        [field: SerializeField] public List<TextAsset> SourceCsvFiles { get; private set; } = new List<TextAsset>();
        [field: SerializeField] public List<CsvColumnDefinition> Columns { get; private set; } = new List<CsvColumnDefinition>();
        [field: SerializeField] public CompiledCsvTableSO<T> CompiledTable { get; set; }
        [field: SerializeField] public bool AutoCompileInEditor { get; private set; }

        [field: SerializeField]
        [field: HideInInspector]
        public int SourceSignature { get; private set; }

        private void OnValidate()
        {
            int newSignature = ComputeSourceSignature();
            if (newSignature != SourceSignature)
            {
                SourceSignature = newSignature;
                SyncColumnsFromSources();
            }

#if UNITY_EDITOR
            if (AutoCompileInEditor)
            {
                CompileInEditor();
            }
#endif
        }

        /// <summary>
        ///     Override this to provide the row → T deserializer for your domain type.
        /// </summary>
        protected abstract IRowDeserializer<T> GetDeserializer();

        public void CompileInEditor()
        {
#if UNITY_EDITOR
            _ = CompiledTable;
            EditorImportBridge.Compile(this);
#endif
        }

        private void SyncColumnsFromSources()
        {
            HashSet<string> incomingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ICsvParser parser = new RobustCsvParser();

            foreach (TextAsset source in SourceCsvFiles)
            {
                if (source == null)
                {
                    continue;
                }

                IReadOnlyList<IReadOnlyList<string>> rows = parser.Parse(source.text);
                if (rows.Count == 0)
                {
                    continue;
                }

                IReadOnlyList<string> header = rows[0];
                foreach (string s in header)
                {
                    string columnName = s?.Trim();
                    if (!string.IsNullOrWhiteSpace(columnName))
                    {
                        incomingColumns.Add(columnName);
                    }
                }
            }

            foreach (string columnName in incomingColumns)
            {
                if (FindColumn(columnName) == null)
                {
                    Columns.Add(new CsvColumnDefinition(columnName));
                }
            }
        }

        protected CsvColumnDefinition FindColumn(string columnName)
        {
            foreach (CsvColumnDefinition column in Columns)
            {
                if (column != null && string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return column;
                }
            }

            return null;
        }

        private int ComputeSourceSignature()
        {
            unchecked
            {
                int hash = 17;
                foreach (TextAsset source in SourceCsvFiles)
                {
                    hash = hash * 31 + (source != null ? source.GetInstanceID() : 0);
                }

                return hash;
            }
        }
    }
}
