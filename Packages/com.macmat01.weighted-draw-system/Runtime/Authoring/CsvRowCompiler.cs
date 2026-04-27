using System;
using System.Collections.Generic;
using Csv;
namespace Authoring
{
    /// <summary>
    ///     Generic CSV compiler that deserializes rows into any type T using a custom IRowDeserializer.
    /// </summary>
    public sealed class CsvRowCompiler<T> where T : class
    {
        private readonly ICsvParser csvParser;
        private readonly IRowDeserializer<T> deserializer;

        public CsvRowCompiler(ICsvParser csvParser, IRowDeserializer<T> deserializer)
        {
            this.csvParser = csvParser ?? new RobustCsvParser();
            this.deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        }

        public List<T> Compile(IReadOnlyList<string> csvContents, IReadOnlyList<CsvColumnDefinition> columns)
        {
            List<T> results = new List<T>();
            if (csvContents == null)
            {
                return results;
            }

            for (int sourceIndex = 0; sourceIndex < csvContents.Count; sourceIndex++)
            {
                IReadOnlyList<IReadOnlyList<string>> rows = csvParser.Parse(csvContents[sourceIndex]);
                if (rows.Count == 0)
                {
                    continue;
                }

                IReadOnlyList<string> header = rows[0];
                ValidateRequiredColumns(header, columns, sourceIndex);

                for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
                {
                    Dictionary<string, string> rowDict = ToRowDictionary(header, rows[rowIndex]);
                    T item = deserializer.DeserializeRow(rowDict, rowIndex + 1);
                    if (item != null)
                    {
                        results.Add(item);
                    }
                }
            }

            return results;
        }

        private static void ValidateRequiredColumns(
            IReadOnlyList<string> header,
            IReadOnlyList<CsvColumnDefinition> schemaColumns,
            int sourceIndex)
        {
            if (schemaColumns == null)
            {
                return;
            }

            HashSet<string> headerSet = new HashSet<string>(header, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < schemaColumns.Count; i++)
            {
                CsvColumnDefinition column = schemaColumns[i];
                if (column == null || !column.IsRequired || string.IsNullOrWhiteSpace(column.ColumnName))
                {
                    continue;
                }

                if (!headerSet.Contains(column.ColumnName))
                {
                    throw new InvalidOperationException(
                        $"CSV source #{sourceIndex + 1} is missing required column '{column.ColumnName}'.");
                }
            }
        }

        private static Dictionary<string, string> ToRowDictionary(
            IReadOnlyList<string> header,
            IReadOnlyList<string> row)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Count; i++)
            {
                string key = header[i]?.Trim();
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                string value = i < row.Count ? row[i] : string.Empty;
                result[key] = value;
            }

            return result;
        }
    }
}
