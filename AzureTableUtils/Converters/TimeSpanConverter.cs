using System.Globalization;

namespace WebGate.Azure.TableUtils.Converter;

/// <summary>
/// Stores <see cref="TimeSpan"/> using invariant <see cref="TimeSpan.Parse(string, IFormatProvider)"/> format.
/// </summary>
public class TimeSpanConverter : IConverter
{
    /// <inheritdoc />
    public bool IsType(Type type)
    {
        return type == typeof(TimeSpan) || type == typeof(TimeSpan?);
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

        return TimeSpan.Parse(value, CultureInfo.InvariantCulture);
    }
}
