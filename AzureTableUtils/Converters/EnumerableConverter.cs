using Newtonsoft.Json;

namespace WebGate.Azure.TableUtils.Converter;

/// <summary>
/// Serializes <see cref="IEnumerable{T}"/> (except string/byte[]) to JSON in a single string column.
/// </summary>
public class EnumerableConverter : IConverter
{
    /// <inheritdoc />
    public bool IsType(Type type)
    {
        return type.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            && type.Name != "Byte[]"
            && type.Name != "String";
    }

    /// <inheritdoc />
    public string GetValue(Type type, object value)
    {
        return JsonConvert.SerializeObject(value);
    }

    /// <inheritdoc />
    public object? BuildValue(string? value, Type type)
    {
        if (!string.IsNullOrEmpty(value))
        {
            return JsonConvert.DeserializeObject(value, type);
        }

        return null;
    }
}
