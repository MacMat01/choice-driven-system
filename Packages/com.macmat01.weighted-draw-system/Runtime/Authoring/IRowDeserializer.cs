using System.Collections.Generic;
namespace Authoring
{
    /// <summary>
    ///     Defines how to deserialize a CSV row into a runtime object.
    ///     Implement this interface to map your custom columns to your domain types.
    /// </summary>
    public interface IRowDeserializer<T> where T : class
    {
        /// <summary>
        ///     Maps a CSV row (column name => string value) into an object of type T.
        ///     Return null to skip the row; throw an exception for critical errors.
        /// </summary>
        T DeserializeRow(IReadOnlyDictionary<string, string> rowData, int rowNumber);
    }
}
