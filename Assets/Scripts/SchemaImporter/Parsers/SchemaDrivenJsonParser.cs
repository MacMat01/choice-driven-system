using System;
using System.Collections.Generic;
using System.Text;
using SchemaImporter.Schema;
using UnityEngine;
namespace SchemaImporter.Parsers
{
    /// <summary>
    ///     Schema-driven JSON parser that reads JSON objects according to a DataSchemaSO definition.
    ///     Supports either a single object root or an array of objects.
    /// </summary>
    public sealed class SchemaDrivenJsonParser
    {
        public static List<DataRecord> Parse(string rawJson, DataSchemaSO schema)
        {
            List<DataRecord> results = new List<DataRecord>();
            if (string.IsNullOrWhiteSpace(rawJson) || schema == null || !TryExtractObjectPayloads(rawJson, out List<string> objectPayloads))
            {
                return results;
            }

            for (int i = 0; i < objectPayloads.Count; i++)
            {
                int itemNumber = i + 1;
                if (!TryParseObject(objectPayloads[i], schema, itemNumber, out DataRecord record))
                {
                    continue;
                }

                results.Add(record);
            }

            return results;
        }

        private static bool TryExtractObjectPayloads(string rawJson, out List<string> objectPayloads)
        {
            objectPayloads = new List<string>();
            string trimmed = rawJson.Trim();
            if (trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                objectPayloads.AddRange(SplitTopLevelObjects(trimmed));
                return true;
            }

            if (trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                objectPayloads.Add(trimmed);
                return true;
            }

            Debug.LogWarning("SchemaDrivenJsonParser: JSON must start with '{' or '['.");
            return false;
        }

        private static bool TryParseObject(string payload, DataSchemaSO schema, int itemNumber, out DataRecord record)
        {
            record = null;
            Dictionary<string, string> properties = ParseObjectProperties(payload);
            DataRecord parsedRecord = new DataRecord();
            bool hasRequiredFieldErrors = false;

            foreach (ColumnDefinition column in schema.Columns)
            {
                if (!TryParseColumnValue(column, properties, itemNumber, parsedRecord))
                {
                    hasRequiredFieldErrors = true;
                }
            }

            if (hasRequiredFieldErrors)
            {
                Debug.LogWarning($"SchemaDrivenJsonParser: Skipping item {itemNumber} due to missing required fields.");
                return false;
            }

            record = parsedRecord;
            return true;
        }

        private static bool TryParseColumnValue(ColumnDefinition column, IReadOnlyDictionary<string, string> properties, int itemNumber, DataRecord record)
        {
            if (column == null || string.IsNullOrWhiteSpace(column.ColumnName))
            {
                return true;
            }

            if (!properties.TryGetValue(column.ColumnName, out string rawValue))
            {
                if (column.IsRequired)
                {
                    Debug.LogError($"SchemaDrivenJsonParser: Required field '{column.ColumnName}' not found at item {itemNumber}.");
                    return false;
                }

                return true;
            }

            if (column.IsRequired && string.IsNullOrWhiteSpace(rawValue))
            {
                Debug.LogError($"SchemaDrivenJsonParser: Required field '{column.ColumnName}' is empty at item {itemNumber}.");
                return false;
            }

            object parsedValue = SchemaValueParser.ParseJsonValue(rawValue, column.DataType, column.ColumnName, itemNumber);
            record.SetField(column.ColumnName, parsedValue);
            return true;
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

            ExpandNestedPropertyAliases(properties);

            return properties;
        }

        private static void ExpandNestedPropertyAliases(Dictionary<string, string> properties)
        {
            if (properties == null || properties.Count == 0)
            {
                return;
            }

            List<KeyValuePair<string, string>> rootEntries = new List<KeyValuePair<string, string>>(properties);
            foreach (KeyValuePair<string, string> entry in rootEntries)
            {
                if (!TryParseNestedObject(entry.Value, out Dictionary<string, string> nested))
                {
                    continue;
                }

                List<string> rootPath = SplitNameTokens(entry.Key);
                ExpandNestedObjectIntoAliases(properties, rootPath, nested);
            }
        }

        private static void ExpandNestedObjectIntoAliases(
            Dictionary<string, string> destination,
            List<string> path,
            Dictionary<string, string> nestedProperties)
        {
            int siblingIndex = 1;
            foreach (KeyValuePair<string, string> nestedEntry in nestedProperties)
            {
                string rawLeafName = nestedEntry.Key;
                List<string> currentPath = new List<string>(path);
                currentPath.AddRange(SplitNameTokens(rawLeafName));
                if (TryParseNestedObject(nestedEntry.Value, out Dictionary<string, string> childObject))
                {
                    ExpandNestedObjectIntoAliases(destination, currentPath, childObject);
                    siblingIndex++;
                    continue;
                }

                AddPropertyAlias(destination, string.Join("_", currentPath), nestedEntry.Value);

                if (path.Count >= 1)
                {
                    // Preserve unsplit leaf names for schemas using exact camel/pascal tokens (e.g. Left_FollowUp).
                    AddPropertyAlias(destination, path[0] + "_" + rawLeafName, nestedEntry.Value);
                }

                if (currentPath.Count >= 2)
                {
                    // Common schema style: Parent_Leaf (e.g. Left_Answer from Left_Choice.Answer)
                    string firstAndLast = currentPath[0] + "_" + currentPath[^1];
                    AddPropertyAlias(destination, firstAndLast, nestedEntry.Value);
                }

                if (currentPath.Count >= 3)
                {
                    // Generic numbered schema support: Prefix_Attribute1 style mappings.
                    string prefix = currentPath[0];
                    // Use the current container name (path tail) instead of leaf-adjacent token.
                    // This keeps numbering stable for compound leaf keys like Accademic_Performance.
                    string parentName = path[^1];
                    string singularParent = SingularizeToken(parentName);

                    AddPropertyAlias(destination, $"{prefix}_{parentName}{siblingIndex}", nestedEntry.Value);
                    AddPropertyAlias(destination, $"{prefix}_{singularParent}{siblingIndex}", nestedEntry.Value);
                }

                siblingIndex++;
            }
        }

        private static bool TryParseNestedObject(string rawValue, out Dictionary<string, string> nested)
        {
            nested = null;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return false;
            }

            string trimmed = rawValue.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal) || !trimmed.EndsWith("}", StringComparison.Ordinal))
            {
                return false;
            }

            nested = ParseObjectProperties(trimmed);
            return nested.Count > 0;
        }

        private static void AddPropertyAlias(Dictionary<string, string> destination, string alias, string value)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                return;
            }

            destination.TryAdd(alias, value);
        }

        private static string SingularizeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            return token.EndsWith("s", StringComparison.OrdinalIgnoreCase)
                ? token[..^1]
                : token;
        }

        private static List<string> SplitNameTokens(string rawName)
        {
            List<string> tokens = new List<string>();
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return tokens;
            }

            StringBuilder token = new StringBuilder();
            foreach (char c in rawName)
            {
                bool isSeparator = c == '_' || c == '-' || char.IsWhiteSpace(c);

                if (isSeparator)
                {
                    if (token.Length > 0)
                    {
                        tokens.Add(token.ToString());
                        token.Length = 0;
                    }

                    continue;
                }

                if (char.IsUpper(c) && token.Length > 0)
                {
                    tokens.Add(token.ToString());
                    token.Length = 0;
                }

                token.Append(c);
            }

            if (token.Length > 0)
            {
                tokens.Add(token.ToString());
            }

            if (tokens.Count == 0)
            {
                tokens.Add(rawName);
            }

            return tokens;
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
