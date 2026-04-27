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
            bool inQuotes = false;

            for (int i = 0; i < rawCsv.Length; i++)
            {
                char current = rawCsv[i];

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
                    case false when current is '\n' or '\r':
                    {
                        currentRow.Add(cellBuilder.ToString());
                        cellBuilder.Length = 0;
                        rows.Add(currentRow);
                        currentRow = new List<string>();

                        if (current == '\r' && i + 1 < rawCsv.Length && rawCsv[i + 1] == '\n')
                        {
                            i++;
                        }

                        continue;
                    }
                    default:
                        cellBuilder.Append(current);
                        break;
                }

            }

            if (cellBuilder.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(cellBuilder.ToString());
                rows.Add(currentRow);
            }

            return rows;
        }
    }
}
