using System;
using System.Collections.Generic;
using System.Globalization;
namespace Conditions
{
    public interface IGameStateReader
    {
        bool TryGetValue(string key, out float value);
    }

    public sealed class DictionaryGameStateReader : IGameStateReader
    {
        private readonly IReadOnlyDictionary<string, object> values;

        public DictionaryGameStateReader(IReadOnlyDictionary<string, object> values)
        {
            this.values = values;
        }

        public bool TryGetValue(string key, out float value)
        {
            value = 0f;
            if (values == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            foreach (KeyValuePair<string, object> pair in values)
            {
                if (!string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return TryConvertToFloat(pair.Value, out value);
            }

            return false;
        }

        private static bool TryConvertToFloat(object rawValue, out float value)
        {
            value = 0f;
            switch (rawValue)
            {
                case null:
                    return false;
                case float f:
                    value = f;
                    return true;
                case double d:
                    value = (float)d;
                    return true;
                case int i:
                    value = i;
                    return true;
                case long l:
                    value = l;
                    return true;
                case bool b:
                    value = b ? 1f : 0f;
                    return true;
                case string s when float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedFloat):
                    value = parsedFloat;
                    return true;
                case string s when bool.TryParse(s, out bool parsedBool):
                    value = parsedBool ? 1f : 0f;
                    return true;
                default:
                    return false;
            }
        }
    }
}
