using System.Collections.Generic;
using SchemaImporter.Schema;

namespace SchemaImporter.Parsers
{
    /// <summary>
    ///     Extension point for schema-based data parsers.
    /// </summary>
    public interface ISchemaDataParser
    {
        bool CanParse(string normalizedExtension);
        List<DataRecord> Parse(string rawText, DataSchemaSO schema);
    }
}

