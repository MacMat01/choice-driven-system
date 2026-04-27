using System.Collections.Generic;
using UnityEngine;
namespace Authoring
{
    /// <summary>
    ///     Generic container for compiled CSV data of any type T.
    ///     Does not include any draw logic; that is the responsibility of the consumer.
    /// </summary>
    public class CompiledCsvTableSO<T> : ScriptableObject where T : class
    {
        [SerializeField] private List<T> rows = new List<T>();

        public IReadOnlyList<T> Rows => rows;

        public void SetRows(List<T> values)
        {
            rows = values ?? new List<T>();
        }
    }
}
