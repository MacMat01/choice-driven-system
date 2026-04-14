using System;
using System.Collections.Generic;
using System.Text;
using SchemaImporter.Schema;
using UnityEngine;
namespace SchemaImporter.Parsers
{
    /// <summary>
    ///     Schema-driven CSV parser that reads CSV data according to a DataSchemaSO definition.
    ///     Returns a list of DataRecord objects with dynamically typed fields.
    /// </summary>
    public sealed class CsvDataParser
    {
        public List<DataRecord> Parse(string rawCsv, DataSchemaSO schema)
        {
            List<DataRecord> results = new List<DataRecord>();

            if (string.IsNullOrWhiteSpace(rawCsv) || schema == null)
            {
                return results;
            }

            List<string> records = SplitRecords(rawCsv);
            if (records.Count == 0)
            {
                return results;
            }

            List<string> headers = ParseRecord(records[0]);
            Dictionary<string, int> columnIndexMap = BuildColumnIndexMap(headers);
            HashSet<string> missingSchemaColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int rowIndex = 1; rowIndex < records.Count; rowIndex++)
            {
                int rowNumber = rowIndex + 1;
                if (TryParseDataRow(records[rowIndex], rowNumber, schema, columnIndexMap, missingSchemaColumns, out DataRecord dataRecord))
                {
                    results.Add(dataRecord);
                }
            }

            return results;
        }

        private static Dictionary<string, int> BuildColumnIndexMap(List<string> headers)
        {
            Dictionary<string, int> columnIndexMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
            {
                string header = headers[i]?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(header))
                {
                    columnIndexMap[header] = i;
                }
            }

            return columnIndexMap;
        }

        private static bool TryParseDataRow(
            string record,
            int rowNumber,
            DataSchemaSO schema,
            IReadOnlyDictionary<string, int> columnIndexMap,
            ISet<string> missingSchemaColumns,
            out DataRecord dataRecord)
        {
            dataRecord = null;
            if (string.IsNullOrWhiteSpace(record))
            {
                return false;
            }

            List<string> values = ParseRecord(record);
            DataRecord parsedRecord = new DataRecord();
            bool rowHasRequiredFieldErrors = false;

            foreach (ColumnDefinition columnDef in schema.Columns)
            {
                if (!TryParseColumnValue(columnDef, values, rowNumber, columnIndexMap, missingSchemaColumns, parsedRecord))
                {
                    rowHasRequiredFieldErrors = true;
                }
            }

            if (rowHasRequiredFieldErrors)
            {
                Debug.LogWarning($"CsvDataParser: Skipping row {rowNumber} due to missing required fields.");
                return false;
            }

            dataRecord = parsedRecord;
            return true;
        }

        private static bool TryParseColumnValue(
            ColumnDefinition columnDef,
            IReadOnlyList<string> values,
            int rowNumber,
            IReadOnlyDictionary<string, int> columnIndexMap,
            ISet<string> missingSchemaColumns,
            DataRecord dataRecord)
        {
            if (columnDef == null || string.IsNullOrWhiteSpace(columnDef.ColumnName))
            {
                return true;
            }

            if (!columnIndexMap.TryGetValue(columnDef.ColumnName, out int columnIndex))
            {
                if (missingSchemaColumns.Add(columnDef.ColumnName))
                {
                    Debug.LogWarning($"CsvDataParser: Column '{columnDef.ColumnName}' from schema not found in CSV headers. It will be skipped.");
                }

                if (columnDef.IsRequired)
                {
                    Debug.LogError($"CsvDataParser: Required column '{columnDef.ColumnName}' not found in CSV at row {rowNumber}.");
                    return false;
                }

                return true;
            }

            if (columnIndex >= values.Count)
            {
                if (columnDef.IsRequired)
                {
                    Debug.LogError($"CsvDataParser: Required column '{columnDef.ColumnName}' is empty at row {rowNumber}.");
                    return false;
                }

                return true;
            }

            string cellValue = values[columnIndex];
            if (columnDef.IsRequired && string.IsNullOrWhiteSpace(cellValue))
            {
                Debug.LogError($"CsvDataParser: Required column '{columnDef.ColumnName}' is empty at row {rowNumber}.");
                return false;
            }

            object parsedValue = SchemaValueParser.ParseCsvCell(cellValue, columnDef.DataType, columnDef.ColumnName, rowNumber);
            dataRecord.SetField(columnDef.ColumnName, parsedValue);
            return true;
        }

        private List<string> SplitRecords(string rawText)
        {
            List<string> records = new List<string>();
            bool inQuotes = false;
            int startIndex = 0;

            for (int i = 0; i < rawText.Length; i++)
            {
                char c = rawText[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < rawText.Length && rawText[i + 1] == '"')
                    {
                        i++;
                        continue;
                    }

                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && (c == '\n' || c == '\r'))
                {
                    string record = rawText.Substring(startIndex, i - startIndex);
                    if (!string.IsNullOrWhiteSpace(record))
                    {
                        records.Add(record);
                    }

                    if (c == '\r' && i + 1 < rawText.Length && rawText[i + 1] == '\n')
                    {
                        i++;
                    }

                    startIndex = i + 1;
                }
            }

            if (startIndex < rawText.Length)
            {
                string finalRecord = rawText[startIndex..];
                if (!string.IsNullOrWhiteSpace(finalRecord))
                {
                    records.Add(finalRecord);
                }
            }

            return records;
        }

        private static List<string> ParseRecord(string record)
        {
            List<string> values = new List<string>();
            bool inQuotes = false;
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < record.Length; i++)
            {
                char c = record[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < record.Length && record[i + 1] == '"')
                    {
                        builder.Append('"');
                        i++;
                        continue;
                    }

                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && c == ',')
                {
                    values.Add(builder.ToString());
                    builder.Length = 0;
                    continue;
                }

                builder.Append(c);
            }

            values.Add(builder.ToString());
            return values;
        }
    }
}
