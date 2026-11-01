using System.Globalization;

namespace WebGate.Azure.TableUtils.Converter;

/// <summary>
/// Stores <see cref="decimal"/> as an invariant-culture string column.
/// </summary>
public class DecimalConverter : IConverter
{
    /// <inheritdoc />
    public bool IsType(Type type)
    {
        return type == typeof(decimal) || type == typeof(decimal?);
    }

    /// <inheritdoc />
    public string GetValue(Type type, object value)
    {
        return ((decimal)value).ToString(CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public object? BuildValue(string? value, Type type)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return decimal.Parse(value, CultureInfo.InvariantCulture);
    }
}
