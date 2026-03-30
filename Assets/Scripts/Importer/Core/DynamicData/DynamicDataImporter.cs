using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Importer.Core.DynamicData
{
    /// <summary>
    ///     Central manager that routes raw data to the matching dynamic parser using file extension.
    /// </summary>
    public static class DynamicDataImporter
    {
        private static readonly SchemaDrivenCsvParser CsvParser = new SchemaDrivenCsvParser();
        private static readonly SchemaDrivenJsonParser JsonParser = new SchemaDrivenJsonParser();

        public static List<DataRecord> ImportRaw(string rawText, string extension, DataSchemaSO schema)
        {
            if (string.IsNullOrWhiteSpace(rawText) || schema == null)
            {
                return new List<DataRecord>();
            }

            string normalizedExtension = NormalizeExtension(extension, rawText);
            if (string.Equals(normalizedExtension, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                return CsvParser.Parse(rawText, schema);
            }

            if (string.Equals(normalizedExtension, ".json", StringComparison.OrdinalIgnoreCase))
            {
                return JsonParser.Parse(rawText, schema);
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

        public static List<DataRecord> ImportFromTextAsset(TextAsset textAsset, DataSchemaSO schema)
        {
            if (textAsset == null)
            {
                return new List<DataRecord>();
            }

            string extension = Path.GetExtension(textAsset.name);
            return ImportRaw(textAsset.text, extension, schema);
        }

        public static List<DataRecord> ImportFromFilePath(string filePath, DataSchemaSO schema)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return new List<DataRecord>();
            }

            string rawText = File.ReadAllText(filePath);
            string extension = Path.GetExtension(filePath);
            return ImportRaw(rawText, extension, schema);
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
    }
}
