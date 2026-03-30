using System;
using System.Globalization;
using UnityEngine;
namespace Importer.Core.DynamicData
{
    /// <summary>
    ///     Shared type conversion helpers used by schema-driven CSV and JSON parsers.
    /// </summary>
    static class SchemaValueParser
    {
        public static object ParseCsvCell(string cellValue, ColumnDataType dataType, string columnName, int rowNumber)
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
                        return ParseBoolOrDefault(trimmed, false, $"SchemaDrivenCsvParser: Failed to parse '{columnName}' as Bool at row {rowNumber}. Value: '{cellValue}'. Defaulting to false.");
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

        public static object ParseJsonValue(string rawValue, ColumnDataType dataType, string columnName, int itemIndex)
        {
            string value = rawValue ?? string.Empty;

            switch (dataType)
            {
                case ColumnDataType.String:
                    return rawValue;
                case ColumnDataType.Int:
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                    {
                        return intValue;
                    }

                    Debug.LogWarning($"SchemaDrivenJsonParser: Failed to parse '{columnName}' as Int at item {itemIndex}. Value: '{value}'. Defaulting to 0.");
                    return 0;
                case ColumnDataType.Float:
                    if (float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float floatValue))
                    {
                        return floatValue;
                    }

                    Debug.LogWarning($"SchemaDrivenJsonParser: Failed to parse '{columnName}' as Float at item {itemIndex}. Value: '{value}'. Defaulting to 0.");
                    return 0f;
                case ColumnDataType.Bool:
                    return ParseBoolOrDefault(value, false, $"SchemaDrivenJsonParser: Failed to parse '{columnName}' as Bool at item {itemIndex}. Value: '{value}'. Defaulting to false.");
                case ColumnDataType.ConditionList:
                    return ConditionParserUtility.Parse(value);
                default:
                    return null;
            }
        }

        private static bool ParseBoolOrDefault(string value, bool defaultValue, string warningMessage)
        {
            if (bool.TryParse(value, out bool parsedBool))
            {
                return parsedBool;
            }

            switch (value)
            {
                case "0":
                    return false;
                case "1":
                    return true;
                default:
                    Debug.LogWarning(warningMessage);
                    return defaultValue;
            }
        }
    }
}
