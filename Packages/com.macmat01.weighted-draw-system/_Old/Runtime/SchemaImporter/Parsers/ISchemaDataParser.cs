using System;
using System.Collections.Generic;
using _Old.Runtime.SchemaImporter.Schema;
namespace _Old.Runtime.SchemaImporter.Parsers
{
    /// <summary>
    ///     Extension point for schema-based data parsers.
    /// </summary>
    [Obsolete("Legacy _Old API. Use the new MacMat01.WeightedDrawSystem package APIs.", false)]
    public interface ISchemaDataParser
    {
        bool             CanParse(string normalizedExtension);
        List<DataRecord> Parse(string rawText, DataSchemaSO schema);
    }
}
