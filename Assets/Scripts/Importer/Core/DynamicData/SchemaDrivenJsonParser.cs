using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
namespace Importer.Core.DynamicData
{
    /// <summary>
    ///     Schema-driven JSON parser that reads flat JSON objects according to a DataSchemaSO definition.
    ///     Supports either a single object root or an array of objects.
    /// </summary>
    public sealed class SchemaDrivenJsonParser
    {
        public List<DataRecord> Parse(string rawJson, DataSchemaSO schema)
        {
            List<DataRecord> results = new List<DataRecord>();
            if (string.IsNullOrWhiteSpace(rawJson) || schema == null)
            {
                return results;
            }

            string trimmed = rawJson.Trim();
            List<string> objectPayloads = new List<string>();

            if (trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                objectPayloads.AddRange(SplitTopLevelObjects(trimmed));
            }
            else if (trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                objectPayloads.Add(trimmed);
            }
            else
            {
                Debug.LogWarning("SchemaDrivenJsonParser: JSON must start with '{' or '['.");
                return results;
            }

            for (int i = 0; i < objectPayloads.Count; i++)
            {
                Dictionary<string, string> properties = ParseObjectProperties(objectPayloads[i]);
                DataRecord record = new DataRecord();
                bool objectHasErrors = false;

                foreach (ColumnDefinition column in schema.Columns)
                {
                    if (column == null || string.IsNullOrWhiteSpace(column.ColumnName))
                    {
                        continue;
                    }

                    if (!properties.TryGetValue(column.ColumnName, out string rawValue))
                    {
                        // Check if this required column is missing
                        if (column.IsRequired)
                        {
                            Debug.LogError($"SchemaDrivenJsonParser: Required field '{column.ColumnName}' not found at item {i + 1}.");
                            objectHasErrors = true;
                        }
                        continue;
                    }

                    // Check if required field is null or empty
                    if (column.IsRequired && string.IsNullOrWhiteSpace(rawValue))
                    {
                        Debug.LogError($"SchemaDrivenJsonParser: Required field '{column.ColumnName}' is empty at item {i + 1}.");
                        objectHasErrors = true;
                        continue;
                    }

                    object parsedValue = ParseValue(rawValue, column.DataType, column.ColumnName, i + 1);
                    record.SetField(column.ColumnName, parsedValue);
                }

                // Skip this record if it has required field errors
                if (objectHasErrors)
                {
                    Debug.LogWarning($"SchemaDrivenJsonParser: Skipping item {i + 1} due to missing required fields.");
                    continue;
                }

                results.Add(record);
            }

            return results;
        }

        private static object ParseValue(string rawValue, ColumnDataType dataType, string columnName, int itemIndex)
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
                    if (bool.TryParse(value, out bool boolValue))
                    {
                        return boolValue;
                    }

                    switch (value)
                    {
                        case "0":
                            return false;
                        case "1":
                            return true;
                        default:
                            Debug.LogWarning($"SchemaDrivenJsonParser: Failed to parse '{columnName}' as Bool at item {itemIndex}. Value: '{value}'. Defaulting to false.");
                            return false;
                    }

                case ColumnDataType.ConditionList:
                    return ConditionParserUtility.Parse(value);

                default:
                    return null;
            }
        }

        private static List<string> SplitTopLevelObjects(string jsonArray)
        {
            List<string> objects = new List<string>();
            bool inString = false;
            bool escape = false;
            int depth = 0;
            int objectStart = -1;

            for (int i = 0; i < jsonArray.Length; i++)
            {
                char c = jsonArray[i];

                if (inString)
                {
                    if (escape)
                    {
                        escape = false;
                    }
                    else
                    {
                        switch (c)
                        {
                            case '\\':
                                escape = true;
                                break;
                            case '"':
                                inString = false;
                                break;
                        }
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                        inString = true;
                        continue;
                    case '{':
                    {
                        if (depth == 0)
                        {
                            objectStart = i;
                        }

                        depth++;
                        continue;
                    }
                    case '}':
                    {
                        depth--;
                        if (depth == 0 && objectStart >= 0)
                        {
                            objects.Add(jsonArray.Substring(objectStart, i - objectStart + 1));
                            objectStart = -1;
                        }
                        break;
                    }
                }

            }

            return objects;
        }

        private static Dictionary<string, string> ParseObjectProperties(string jsonObject)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int i = 0;

            SkipWhitespace(jsonObject, ref i);
            if (i >= jsonObject.Length || jsonObject[i] != '{')
            {
                return properties;
            }

            i++;

            while (i < jsonObject.Length)
            {
                SkipWhitespace(jsonObject, ref i);
                if (i < jsonObject.Length && jsonObject[i] == '}')
                {
                    break;
                }

                if (!TryReadJsonString(jsonObject, ref i, out string key))
                {
                    break;
                }

                SkipWhitespace(jsonObject, ref i);
                if (i >= jsonObject.Length || jsonObject[i] != ':')
                {
                    break;
                }

                i++;
                SkipWhitespace(jsonObject, ref i);

                if (!TryReadJsonValueAsString(jsonObject, ref i, out string value))
                {
                    break;
                }

                properties[key] = value;

                SkipWhitespace(jsonObject, ref i);
                if (i < jsonObject.Length && jsonObject[i] == ',')
                {
                    i++;
                }
            }

            return properties;
        }

        private static bool TryReadJsonString(string text, ref int index, out string result)
        {
            result = string.Empty;
            if (index >= text.Length || text[index] != '"')
            {
                return false;
            }

            index++;
            StringBuilder builder = new StringBuilder();
            bool escape = false;

            while (index < text.Length)
            {
                char c = text[index++];
                if (escape)
                {
                    switch (c)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        default: builder.Append(c); break;
                    }

                    escape = false;
                    continue;
                }

                switch (c)
                {
                    case '\\':
                        escape = true;
                        continue;
                    case '"':
                        result = builder.ToString();
                        return true;
                    default:
                        builder.Append(c);
                        break;
                }

            }

            return false;
        }

        private static bool TryReadJsonValueAsString(string text, ref int index, out string value)
        {
            value = string.Empty;
            if (index >= text.Length)
            {
                return false;
            }

            if (text[index] == '"')
            {
                return TryReadJsonString(text, ref index, out value);
            }

            int start = index;
            int nestedDepth = 0;
            bool inString = false;
            bool escape = false;

            while (index < text.Length)
            {
                char c = text[index];

                if (inString)
                {
                    if (escape)
                    {
                        escape = false;
                    }
                    else
                    {
                        switch (c)
                        {
                            case '\\':
                                escape = true;
                                break;
                            case '"':
                                inString = false;
                                break;
                        }
                    }

                    index++;
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inString = true;
                        index++;
                        continue;
                    case '{':
                    case '[':
                        nestedDepth++;
                        index++;
                        continue;
                }

                if (c is '}' or ']')
                {
                    if (nestedDepth == 0)
                    {
                        break;
                    }

                    nestedDepth--;
                    index++;
                    continue;
                }

                if (nestedDepth == 0 && c == ',')
                {
                    break;
                }

                index++;
            }

            value = text.Substring(start, index - start).Trim();
            if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
            {
                value = null;
            }

            return true;
        }

        private static void SkipWhitespace(string text, ref int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }
        }
    }
}
