using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
namespace Importer.Core.DynamicData
{
    /// <summary>
    ///     Schema-driven CSV parser that reads CSV data according to a DataSchemaSO definition.
    ///     Returns a list of DataRecord objects with dynamically typed fields.
    /// </summary>
    public sealed class SchemaDrivenCsvParser
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
            Dictionary<string, int> columnIndexMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> missingSchemaColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headers.Count; i++)
            {
                string header = headers[i]?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(header))
                {
                    columnIndexMap[header] = i;
                }
            }

            for (int rowIndex = 1; rowIndex < records.Count; rowIndex++)
            {
                string record = records[rowIndex];
                if (string.IsNullOrWhiteSpace(record))
                {
                    continue;
                }

                List<string> values = ParseRecord(record);
                DataRecord dataRecord = new DataRecord();

                foreach (ColumnDefinition columnDef in schema.Columns)
                {
                    if (!columnIndexMap.TryGetValue(columnDef.ColumnName, out int columnIndex))
                    {
                        if (missingSchemaColumns.Add(columnDef.ColumnName))
                        {
                            Debug.LogWarning($"SchemaDrivenCsvParser: Column '{columnDef.ColumnName}' from schema not found in CSV headers. It will be skipped.");
                        }

                        continue;
                    }

                    if (columnIndex >= values.Count)
                    {
                        continue;
                    }

                    string cellValue = values[columnIndex];
                    object parsedValue = ParseCellValue(cellValue, columnDef.DataType, columnDef.ColumnName, rowIndex + 1);
                    dataRecord.SetField(columnDef.ColumnName, parsedValue);
                }

                results.Add(dataRecord);
            }

            return results;
        }

        private static object ParseCellValue(string cellValue, ColumnDataType dataType, string columnName, int rowNumber)
        {
            string trimmed = cellValue?.Trim() ?? string.Empty;

            try
            {
                switch (dataType)
                {
                    case ColumnDataType.String:
                        return trimmed;

                    case ColumnDataType.Int:
                        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                        {
                            return intValue;
                        }

                        Debug.LogWarning($"SchemaDrivenCsvParser: Failed to parse '{columnName}' as Int at row {rowNumber}. Value: '{cellValue}'. Defaulting to 0.");
                        return 0;

                    case ColumnDataType.Float:
                        if (float.TryParse(trimmed, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float floatValue))
                        {
                            return floatValue;
                        }

                        Debug.LogWarning($"SchemaDrivenCsvParser: Failed to parse '{columnName}' as Float at row {rowNumber}. Value: '{cellValue}'. Defaulting to 0.");
                        return 0f;

                    case ColumnDataType.Bool:
                        if (bool.TryParse(trimmed, out bool boolValue))
                        {
                            return boolValue;
                        }

                        switch (trimmed)
                        {
                            case "0":
                                return false;
                            case "1":
                                return true;
                            default:
                                Debug.LogWarning($"SchemaDrivenCsvParser: Failed to parse '{columnName}' as Bool at row {rowNumber}. Value: '{cellValue}'. Defaulting to false.");
                                return false;
                        }

                    case ColumnDataType.ConditionList:
                        return ConditionParserUtility.Parse(trimmed);

                    default:
                        Debug.LogWarning($"SchemaDrivenCsvParser: Unknown data type '{dataType}' for column '{columnName}' at row {rowNumber}.");
                        return null;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"SchemaDrivenCsvParser: Exception parsing '{columnName}' at row {rowNumber}: {exception.Message}");
                return null;
            }
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
