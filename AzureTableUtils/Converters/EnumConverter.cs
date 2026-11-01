namespace WebGate.Azure.TableUtils.Converter;

/// <summary>
/// Stores enums as their string name in a string column.
/// </summary>
public class EnumConverter : IConverter
{
    /// <inheritdoc />
    public bool IsType(Type type)
    {
        return type.IsEnum || Nullable.GetUnderlyingType(type)?.IsEnum == true;
    }

    /// <inheritdoc />
    public string GetValue(Type type, object value)
    {
        return value.ToString() ?? "";
    }

    /// <inheritdoc />
    public object? BuildValue(string? value, Type type)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        Type enumType = Nullable.GetUnderlyingType(type) ?? type;
        return Enum.Parse(enumType, value);
    }
}
