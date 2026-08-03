using System.Collections.Concurrent;
using System.Reflection;

namespace WebGate.Azure.TableUtils;

internal static class EntityMapping
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    internal static PropertyInfo[] GetWritableProperties(Type type)
    {
        return PropertyCache.GetOrAdd(type, static t =>
            t.GetProperties().Where(p => p.CanRead && p.CanWrite).ToArray());
    }

    internal static bool IsPrimitiveTableType(Type type)
    {
        return type.IsValueType || type == typeof(string) || type == typeof(byte[]);
    }

    internal static string BuildEntityName(string? path, string id)
    {
        if (string.IsNullOrEmpty(path))
        {
            return id;
        }
        return path + "_" + id;
    }
}
