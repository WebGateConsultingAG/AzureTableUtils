namespace WebGate.Azure.TableUtils.Converter;

/// <summary>
/// Resolves the first <see cref="IConverter"/> that supports a property type.
/// </summary>
public static class ConverterFactory
{
    private static readonly Lazy<List<IConverter>> Converters = new(InitConverters);

    /// <summary>
    /// Finds a converter for <paramref name="type"/>, or <see langword="null"/> if the type is mapped as primitive/nested object.
    /// </summary>
    public static IConverter? FindConverter(Type type)
    {
        return Converters.Value.Find(converter => converter.IsType(type));
    }

    private static List<IConverter> InitConverters()
    {
        return
        [
            new EnumConverter(),
            new TimeSpanConverter(),
            new DecimalConverter(),
            new ArrayConverter(),
            new EnumerableConverter()
        ];
    }
}
