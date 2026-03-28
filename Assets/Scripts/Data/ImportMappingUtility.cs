using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
namespace Data
{
    static class ImportMappingUtility
    {
        private static readonly Dictionary<Type, Dictionary<string, MemberInfo>> MemberCache = new Dictionary<Type, Dictionary<string, MemberInfo>>();

        public static Dictionary<string, MemberInfo> GetMappableMembers(Type type)
        {
            if (MemberCache.TryGetValue(type, out Dictionary<string, MemberInfo> cachedMembers))
            {
                return cachedMembers;
            }

            Dictionary<string, MemberInfo> members = new Dictionary<string, MemberInfo>(StringComparer.OrdinalIgnoreCase);

            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo t in fields)
            {
                members[t.Name] = t;
            }

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo property in properties)
            {
                if (property.CanWrite)
                {
                    members[property.Name] = property;
                }
            }

            MemberCache[type] = members;
            return members;
        }

        public static bool TrySetMemberValue(object target, MemberInfo member, string rawValue, out string error)
        {
            error = null;

            Type memberType;
            switch (member)
            {
                case FieldInfo field:
                {
                    memberType = field.FieldType;
                    if (!TryConvertValue(rawValue, memberType, out object converted, out error))
                    {
                        return false;
                    }

                    field.SetValue(target, converted);
                    return true;
                }
                case PropertyInfo property:
                {
                    memberType = property.PropertyType;
                    if (!TryConvertValue(rawValue, memberType, out object converted, out error))
                    {
                        return false;
                    }

                    property.SetValue(target, converted);
                    return true;
                }
                default:
                    error = "Unsupported member type.";
                    return false;
            }

        }

        private static bool TryConvertValue(string rawValue, Type targetType, out object converted, out string error)
        {
            error = null;
            converted = null;

            Type effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            string normalized = rawValue?.Trim();

            if (string.IsNullOrEmpty(normalized))
            {
                converted = targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null
                    ? Activator.CreateInstance(targetType)
                    : null;
                return true;
            }

            try
            {
                if (effectiveType == typeof(string))
                {
                    converted = normalized;
                    return true;
                }

                if (effectiveType == typeof(int))
                {
                    if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                    {
                        converted = intValue;
                        return true;
                    }

                    error = "Invalid integer value.";
                    return false;
                }

                if (effectiveType == typeof(float))
                {
                    if (float.TryParse(normalized, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float floatValue))
                    {
                        converted = floatValue;
                        return true;
                    }

                    error = "Invalid float value.";
                    return false;
                }

                if (effectiveType == typeof(double))
                {
                    if (double.TryParse(normalized, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double doubleValue))
                    {
                        converted = doubleValue;
                        return true;
                    }

                    error = "Invalid double value.";
                    return false;
                }

                if (effectiveType == typeof(bool))
                {
                    if (bool.TryParse(normalized, out bool boolValue))
                    {
                        converted = boolValue;
                        return true;
                    }

                    switch (normalized)
                    {
                        case "0":
                            converted = false;
                            return true;
                        case "1":
                            converted = true;
                            return true;
                        default:
                            error = "Invalid boolean value.";
                            return false;
                    }

                }

                if (effectiveType.IsEnum)
                {
                    if (Enum.TryParse(effectiveType, normalized, true, out object enumValue))
                    {
                        converted = enumValue;
                        return true;
                    }

                    error = "Invalid enum value.";
                    return false;
                }

                converted = Convert.ChangeType(normalized, effectiveType, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }
    }
}
