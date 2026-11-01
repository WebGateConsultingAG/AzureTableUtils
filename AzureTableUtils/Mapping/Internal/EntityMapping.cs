using System.Collections.Concurrent;
using System.Reflection;

namespace WebGate.Azure.TableUtils;

/// <summary>
/// Reflection helpers shared by <see cref="ObjectSerializer"/> and <see cref="ObjectBuilder"/>.
/// </summary>
internal static class EntityMapping
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    /// <summary>Readable and writable properties for mapping, cached per type.</summary>
    internal static PropertyInfo[] GetWritableProperties(Type type)
    {
        return PropertyCache.GetOrAdd(type, static t =>
            t.GetProperties().Where(p => p.CanRead && p.CanWrite).ToArray());
    }

    /// <summary>Types stored as single table columns without flattening or JSON converters.</summary>
    internal static bool IsPrimitiveTableType(Type type)
    {
        return type.IsValueType || type == typeof(string) || type == typeof(byte[]);
    }

    /// <summary>Builds flattened column name (<c>Parent_Child</c>).</summary>
    internal static string BuildEntityName(string? path, string id)
    {
        if (string.IsNullOrEmpty(path))
        {
            return id;
        }

        return path + "_" + id;
    }
}
