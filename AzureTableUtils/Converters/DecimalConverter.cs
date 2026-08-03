using System.Globalization;

namespace WebGate.Azure.TableUtils.Converter;

public class DecimalConverter : IConverter
{
    public bool IsType(Type type)
    {
        return type == typeof(decimal) || type == typeof(decimal?);
    }

    public string GetValue(Type type, object value)
    {
        return ((decimal)value).ToString(CultureInfo.InvariantCulture);
    }

    public object? BuildValue(string? value, Type type)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }
        return decimal.Parse(value, CultureInfo.InvariantCulture);
    }
}
