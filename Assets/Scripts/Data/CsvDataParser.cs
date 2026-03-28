using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
namespace Data
{
    public sealed class CsvDataParser : IDataParser
    {
        public List<T> Parse<T>(string rawText) where T : new()
        {
            List<T> results = new List<T>();

            if (string.IsNullOrWhiteSpace(rawText))
            {
                return results;
            }

            List<string> records = SplitRecords(rawText);
            if (records.Count == 0)
            {
                return results;
            }

            List<string> headers = ParseRecord(records[0]);
            Dictionary<string, MemberInfo> members = ImportMappingUtility.GetMappableMembers(typeof(T));
            HashSet<string> missingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int rowIndex = 1; rowIndex < records.Count; rowIndex++)
            {
                string record = records[rowIndex];
                if (string.IsNullOrWhiteSpace(record))
                {
                    continue;
                }

                List<string> values = ParseRecord(record);
                T instance = new T();

                int columnCount = Math.Min(headers.Count, values.Count);
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    string header = headers[columnIndex]?.Trim();
                    if (string.IsNullOrEmpty(header))
                    {
                        continue;
                    }

                    if (!members.TryGetValue(header, out MemberInfo member))
                    {
                        if (missingColumns.Add(header))
                        {
                            Debug.LogWarning($"CsvDataParser: Column '{header}' does not map to {typeof(T).Name}. It will be ignored.");
                        }
                        continue;
                    }

                    string value = values[columnIndex];
                    if (!ImportMappingUtility.TrySetMemberValue(instance, member, value, out string error))
                    {
                        Debug.LogWarning($"CsvDataParser: Failed to set '{header}' at row {rowIndex + 1}, column {columnIndex + 1}. {error}");
                    }
                }

                results.Add(instance);
            }

            return results;
        }

        private static List<string> SplitRecords(string rawText)
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

                if (!inQuotes && c is '\n' or '\r')
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
