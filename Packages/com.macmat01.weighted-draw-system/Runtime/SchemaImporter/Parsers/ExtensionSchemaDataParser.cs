using System;
using System.Collections.Generic;
using SchemaImporter.Schema;

namespace SchemaImporter.Parsers
{
    /// <summary>
    ///     Shared adapter for binding a file extension to a parsing implementation.
    /// </summary>
    internal sealed class ExtensionSchemaDataParser : ISchemaDataParser
    {
        private readonly string extension;
        private readonly Func<string, DataSchemaSO, List<DataRecord>> parse;

        public ExtensionSchemaDataParser(string extension, Func<string, DataSchemaSO, List<DataRecord>> parse)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new ArgumentException("Extension cannot be null or whitespace.", nameof(extension));
            }

            this.extension = extension.StartsWith(".", StringComparison.Ordinal) ? extension : "." + extension;
            this.parse = parse ?? throw new ArgumentNullException(nameof(parse));
        }

        public bool CanParse(string normalizedExtension)
        {
            return string.Equals(normalizedExtension, extension, StringComparison.OrdinalIgnoreCase);
        }

        public List<DataRecord> Parse(string rawText, DataSchemaSO schema)
        {
            return parse(rawText, schema);
        }
    }
}

