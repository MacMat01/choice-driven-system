using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using _Old.Runtime.SchemaImporter.Schema;
using UnityEngine;
namespace _Old.Runtime.SchemaImporter.Parsers
{
    /// <summary>
    ///     Central manager that routes raw data to the matching dynamic parser using file extension.
    /// </summary>
    [Obsolete("Legacy _Old API. Use the new MacMat01.WeightedDrawSystem package APIs.", false)]
    public static class DynamicDataImporter
    {
        private static readonly List<ISchemaDataParser> Parsers = new List<ISchemaDataParser>
        {
            new ExtensionSchemaDataParser(".csv", static (rawText, schema) => new CsvDataParser().Parse(rawText, schema)),
            new ExtensionSchemaDataParser(".json", static (rawText, schema) => JsonDataParser.Parse(rawText, schema))
        };

        public static void RegisterParser(ISchemaDataParser parser)
        {
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }

            if (Parsers.Contains(parser))
            {
                return;
            }

            Parsers.Insert(0, parser);
        }

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

        /// <summary>
        ///     Imports data using the provided TextAsset source and schema.
        /// </summary>
        public static List<DataRecord> ImportFromTextAsset(TextAsset sourceFile, DataSchemaSO schema)
        {
            if (sourceFile == null)
            {
                throw new ArgumentNullException(nameof(sourceFile), "DynamicDataImporter: A source TextAsset is required.");
            }

            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema), "DynamicDataImporter: A DataSchemaSO is required.");
            }

            string extension = Path.GetExtension(sourceFile.name);
            return ImportRaw(sourceFile.text, extension, schema);
        }

        /// <summary>
        ///     Imports data from an explicit file path and schema.
        /// </summary>
        public static List<DataRecord> ImportFromFilePath(string filePath, DataSchemaSO schema)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("DynamicDataImporter: A valid file path is required.", nameof(filePath));
            }

            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema), "DynamicDataImporter: A DataSchemaSO is required.");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("DynamicDataImporter: Source data file was not found.", filePath);
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

        private static bool TryParseByExtension(string rawText, DataSchemaSO schema, string normalizedExtension, out List<DataRecord> records)
        {
            records = null;
            ISchemaDataParser parser = Parsers.FirstOrDefault(candidate => candidate.CanParse(normalizedExtension));
            if (parser == null)
            {
                return false;
            }

            records = parser.Parse(rawText, schema);
            return true;
        }
    }
}
