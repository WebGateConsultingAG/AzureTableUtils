using System.Globalization;

namespace WebGate.Azure.TableUtils.Converter;

public class TimeSpanConverter : IConverter
{
    public bool IsType(Type type)
    {
        return type == typeof(TimeSpan) || type == typeof(TimeSpan?);
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
        return TimeSpan.Parse(value, CultureInfo.InvariantCulture);
    }
}