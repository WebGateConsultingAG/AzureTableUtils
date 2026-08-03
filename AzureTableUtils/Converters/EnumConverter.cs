namespace WebGate.Azure.TableUtils.Converter;

public class EnumConverter : IConverter
{
    public bool IsType(Type type)
    {
        return type.IsEnum;
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
        ;
        return Enum.Parse(type, value);
    }
}