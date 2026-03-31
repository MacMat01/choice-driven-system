using System;
using System.Collections.Generic;
using System.IO;
using SchemaImporter.Schema;
using UnityEngine;
namespace SchemaImporter.Parsers
{
    /// <summary>
    ///     Central manager that routes raw data to the matching dynamic parser using file extension.
    /// </summary>
    public static class DynamicDataImporter
    {
        private static readonly SchemaDrivenCsvParser CsvParser = new SchemaDrivenCsvParser();
        private static readonly SchemaDrivenJsonParser JsonParser = new SchemaDrivenJsonParser();

        private static List<DataRecord> ImportRaw(string rawText, string extension, DataSchemaSO schema)
        {
            if (string.IsNullOrWhiteSpace(rawText) || schema == null)
            {
                return new List<DataRecord>();
            }

            string normalizedExtension = NormalizeExtension(extension, rawText);
            if (TryParseByExtension(rawText, schema, normalizedExtension, out List<DataRecord> records))
            {
                return records;
            }

            Debug.LogWarning($"DynamicDataImporter: Unsupported extension '{normalizedExtension}'.");
            return new List<DataRecord>();
        }

        /// <summary>
        ///     Imports data from the required TextAsset attached to the schema.
        /// </summary>
        public static List<DataRecord> ImportFromSchema(DataSchemaSO schema)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema), "DynamicDataImporter: A DataSchemaSO is required.");
            }

            if (!schema.HasSourceDataFile())
            {
                throw new InvalidOperationException("DynamicDataImporter: DataSchemaSO requires an assigned CSV or JSON source file.");
            }

            TextAsset sourceFile = schema.SourceDataFile;
            string extension = Path.GetExtension(sourceFile.name);
            return ImportRaw(sourceFile.text, extension, schema);
        }

        private static string NormalizeExtension(string extension, string rawText)
        {
            if (!string.IsNullOrWhiteSpace(extension))
            {
                return extension.StartsWith(".", StringComparison.Ordinal) ? extension : "." + extension;
            }

            string trimmed = rawText?.TrimStart() ?? string.Empty;
            if (trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                return ".json";
            }

            return ".csv";
        }

        private static bool TryParseByExtension(string rawText, DataSchemaSO schema, string normalizedExtension, out List<DataRecord> records)
        {
            records = null;
            if (string.Equals(normalizedExtension, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                records = CsvParser.Parse(rawText, schema);
                return true;
            }

            if (string.Equals(normalizedExtension, ".json", StringComparison.OrdinalIgnoreCase))
            {
                records = SchemaDrivenJsonParser.Parse(rawText, schema);
                return true;
            }

            return false;
        }
    }
}
