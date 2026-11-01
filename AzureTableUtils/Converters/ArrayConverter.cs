using Newtonsoft.Json;

namespace WebGate.Azure.TableUtils.Converter;

/// <summary>
/// Serializes non-byte arrays to JSON in a single string column (Newtonsoft.Json).
/// </summary>
public class ArrayConverter : IConverter
{
    /// <inheritdoc />
    public bool IsType(Type type)
    {
        return type.IsArray && type.Name != "Byte[]";
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
