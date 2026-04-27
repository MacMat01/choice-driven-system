using System;
using System.Collections.Generic;
using System.Globalization;
using _Old.Runtime.SchemaImporter.Schema;
using UnityEngine;
namespace _Old.Runtime.SchemaImporter.Parsers
{
    /// <summary>
    ///     Shared type conversion helpers used by schema-driven CSV and JSON parsers.
    /// </summary>
    static class SchemaValueParser
    {

        private static readonly IReadOnlyDictionary<ColumnDataType, CsvConverter> CsvConverters = new Dictionary<ColumnDataType, CsvConverter>
        {
            {
                ColumnDataType.String, static (value, _, _) => value
            },
            {
                ColumnDataType.Int, static (value, columnName, rowNumber) => TryParseInt(value, out int intValue) ? intValue : WarnAndDefault($"CsvDataParser: Failed to parse '{columnName}' as Int at row {rowNumber}. Value: '{value}'. Defaulting to 0.", 0)
            },
            {
                ColumnDataType.Float, static (value, columnName, rowNumber) => TryParseFloat(value, out float floatValue) ? floatValue : WarnAndDefault($"CsvDataParser: Failed to parse '{columnName}' as Float at row {rowNumber}. Value: '{value}'. Defaulting to 0.", 0f)
            },
            {
                ColumnDataType.Bool, static (value, columnName, rowNumber) => ParseBoolOrDefault(value, false, $"CsvDataParser: Failed to parse '{columnName}' as Bool at row {rowNumber}. Value: '{value}'. Defaulting to false.")
            },
            {
                ColumnDataType.ConditionList, static (value, _, _) => ConditionParserUtility.Parse(value)
            },
            {
                ColumnDataType.WeightColumn, static (value, columnName, rowNumber) => TryParseInt(value, out int weightValue) ? weightValue : WarnAndDefault($"CsvDataParser: Failed to parse '{columnName}' as WeightColumn at row {rowNumber}. Value: '{value}'. Defaulting to 0.", 0)
            }
        };

        private static readonly IReadOnlyDictionary<ColumnDataType, JsonConverter> JsonConverters = new Dictionary<ColumnDataType, JsonConverter>
        {
            {
                ColumnDataType.String, static (value, _, _) => value
            },
            {
                ColumnDataType.Int, static (value, columnName, itemIndex) => TryParseInt(value, out int intValue) ? intValue : WarnAndDefault($"JsonDataParser: Failed to parse '{columnName}' as Int at item {itemIndex}. Value: '{value}'. Defaulting to 0.", 0)
            },
            {
                ColumnDataType.Float, static (value, columnName, itemIndex) => TryParseFloat(value, out float floatValue) ? floatValue : WarnAndDefault($"JsonDataParser: Failed to parse '{columnName}' as Float at item {itemIndex}. Value: '{value}'. Defaulting to 0.", 0f)
            },
            {
                ColumnDataType.Bool, static (value, columnName, itemIndex) => ParseBoolOrDefault(value, false, $"JsonDataParser: Failed to parse '{columnName}' as Bool at item {itemIndex}. Value: '{value}'. Defaulting to false.")
            },
            {
                ColumnDataType.ConditionList, static (value, _, _) => ConditionParserUtility.Parse(value)
            },
            {
                ColumnDataType.WeightColumn, static (value, columnName, itemIndex) => TryParseInt(value, out int weightValue) ? weightValue : WarnAndDefault($"JsonDataParser: Failed to parse '{columnName}' as WeightColumn at item {itemIndex}. Value: '{value}'. Defaulting to 0.", 0)
            }
        };

        public static object ParseCsvCell(string cellValue, ColumnDataType dataType, string columnName, int rowNumber)
        {
            string trimmed = cellValue?.Trim() ?? string.Empty;

            try
            {
                if (CsvConverters.TryGetValue(dataType, out CsvConverter converter))
                {
                    return converter(trimmed, columnName, rowNumber);
                }

                Debug.LogWarning($"CsvDataParser: Unknown data type '{dataType}' for column '{columnName}' at row {rowNumber}.");
                return null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"CsvDataParser: Exception parsing '{columnName}' at row {rowNumber}: {exception.Message}");
                return null;
            }
        }

        public static object ParseJsonValue(string rawValue, ColumnDataType dataType, string columnName, int itemIndex)
        {
            if (JsonConverters.TryGetValue(dataType, out JsonConverter converter))
            {
                return converter(rawValue, columnName, itemIndex);
            }

            return null;
        }

        private static bool TryParseInt(string value, out int parsed)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
        }

        private static bool TryParseFloat(string value, out float parsed)
        {
            return float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out parsed);
        }

        private static T WarnAndDefault<T>(string warningMessage, T defaultValue)
        {
            Debug.LogWarning(warningMessage);
            return defaultValue;
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
        private delegate object CsvConverter(string value, string columnName, int rowNumber);
        private delegate object JsonConverter(string value, string columnName, int itemIndex);
    }
}
