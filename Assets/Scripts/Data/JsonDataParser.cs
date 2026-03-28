using System;
using System.Collections.Generic;
using UnityEngine;
namespace Data
{
    public sealed class JsonDataParser : IDataParser
    {

        public List<T> Parse<T>(string rawText) where T : new()
        {
            List<T> results = new List<T>();
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return results;
            }

            string trimmed = rawText.Trim();

            try
            {
                if (trimmed.StartsWith("[", StringComparison.Ordinal))
                {
                    string wrapped = "{\"Items\":" + trimmed + "}";
                    ArrayWrapper<T> wrapper = JsonUtility.FromJson<ArrayWrapper<T>>(wrapped);
                    if (wrapper?.Items != null)
                    {
                        results.AddRange(wrapper.Items);
                    }

                    return results;
                }

                T item = JsonUtility.FromJson<T>(trimmed);
                if (item is not null)
                {
                    results.Add(item);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"JsonDataParser: Failed to parse JSON into {typeof(T).Name}. {exception.Message}");
            }

            return results;
        }
        [Serializable]
        private sealed class ArrayWrapper<T>
        {
            public T[] Items;
        }
    }
}
