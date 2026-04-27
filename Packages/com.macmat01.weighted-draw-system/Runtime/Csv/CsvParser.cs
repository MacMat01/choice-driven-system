using System;
using System.Collections.Generic;
using System.Text;
namespace Csv
{
    public interface ICsvParser
    {
        IReadOnlyList<IReadOnlyList<string>> Parse(string rawCsv);
    }

    public sealed class RobustCsvParser : ICsvParser
    {
        public IReadOnlyList<IReadOnlyList<string>> Parse(string rawCsv)
        {
            List<IReadOnlyList<string>> rows = new List<IReadOnlyList<string>>();
            if (string.IsNullOrWhiteSpace(rawCsv))
            {
                return rows;
            }

            List<string> currentRow = new List<string>();
            StringBuilder cellBuilder = new StringBuilder();
            StringBuilder rawRecordBuilder = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < rawCsv.Length; i++)
            {
                char current = rawCsv[i];

                if (!inQuotes && current is '\n' or '\r')
                {
                    AddFinalizedRow(rows, currentRow, cellBuilder, rawRecordBuilder);
                    currentRow = new List<string>();

                    if (current == '\r' && i + 1 < rawCsv.Length && rawCsv[i + 1] == '\n')
                    {
                        i++;
                    }

                    continue;
                }

                rawRecordBuilder.Append(current);

                if (current == '"')
                {
                    if (inQuotes && i + 1 < rawCsv.Length && rawCsv[i + 1] == '"')
                    {
                        cellBuilder.Append('"');
                        i++;
                        continue;
                    }

                    inQuotes = !inQuotes;
                    continue;
                }

                switch (inQuotes)
                {
                    case false when current == ',':
                        currentRow.Add(cellBuilder.ToString());
                        cellBuilder.Length = 0;
                        continue;
                    default:
                        cellBuilder.Append(current);
                        break;
                }

            }

            if (cellBuilder.Length > 0 || currentRow.Count > 0 || rawRecordBuilder.Length > 0)
            {
                AddFinalizedRow(rows, currentRow, cellBuilder, rawRecordBuilder);
            }

            return rows;
        }

        private static void AddFinalizedRow(
            ICollection<IReadOnlyList<string>> rows,
            List<string> currentRow,
            StringBuilder cellBuilder,
            StringBuilder rawRecordBuilder)
        {
            currentRow.Add(cellBuilder.ToString());
            cellBuilder.Length = 0;

            List<string> finalizedRow = NormalizeLegacyWrappedRecord(rawRecordBuilder.ToString(), currentRow);
            rows.Add(finalizedRow);
            rawRecordBuilder.Length = 0;
        }

        private static List<string> NormalizeLegacyWrappedRecord(string rawRecord, List<string> parsedRow)
        {
            if (parsedRow == null || parsedRow.Count != 1 || string.IsNullOrWhiteSpace(rawRecord))
            {
                return parsedRow;
            }

            string trimmedRecord = rawRecord.Trim();
            if (trimmedRecord.Length < 2 || trimmedRecord[0] != '"' || trimmedRecord[^1] != '"' || !parsedRow[0].Contains(",", StringComparison.Ordinal))
            {
                return parsedRow;
            }

            // Legacy exporters sometimes wrap the whole row in quotes. Reparse a normalized row for compatibility.
            string normalizedRecord = NormalizeRecord(trimmedRecord);
            List<string> reparsed = ParseRecord(normalizedRecord);
            return reparsed.Count > 1 ? reparsed : parsedRow;
        }

        private static string NormalizeRecord(string record)
        {
            return record.Replace("\"\"", "\"").TrimStart('"');
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
