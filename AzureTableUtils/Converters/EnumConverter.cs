namespace WebGate.Azure.TableUtils.Converter;

public class EnumConverter : IConverter
{
    public bool IsType(Type type)
    {
        return type.IsEnum || Nullable.GetUnderlyingType(type)?.IsEnum == true;
    }

    public string GetValue(Type type, object value)
    {
        return value.ToString() ?? "";
    }

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
