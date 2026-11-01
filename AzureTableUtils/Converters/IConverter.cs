namespace WebGate.Azure.TableUtils.Converter;

/// <summary>
/// Converts between a CLR property type and its Azure Table string representation during serialize/build.
/// </summary>
/// <remarks>
/// Implementations are registered in <see cref="ConverterFactory"/> (enums, decimals, arrays, enumerables, …).
/// </remarks>
public interface IConverter
{
    /// <summary>Returns whether this converter handles <paramref name="type"/>.</summary>
    public bool IsType(Type type);

    /// <summary>Writes a property value to a table-storable string.</summary>
    public string GetValue(Type type, object value);

    /// <summary>Reads a table string back into a property value.</summary>
    public object? BuildValue(string? value, Type type);
}
