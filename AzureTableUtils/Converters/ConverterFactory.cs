namespace WebGate.Azure.TableUtils.Converter;

public static class ConverterFactory
{
    private static readonly Lazy<List<IConverter>> Converters = new(InitConverters);

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
