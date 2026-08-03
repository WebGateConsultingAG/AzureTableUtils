namespace WebGate.Azure.TableUtils.Converter;

public static class ConverterFactory
{
    private static List<IConverter>? converters = null;

    public static IConverter? FindConverter(Type type)
    {
        converters ??= InitConverters();
        return converters.Find(converter => converter.IsType(type));
    }

    private static List<IConverter> InitConverters()
    {
        List<IConverter> list = new List<IConverter>();
        list.Add(new EnumConverter());
        list.Add(new TimeSpanConverter());
        list.Add(new DecimalConverter());
        list.Add(new ArrayConverter());
        list.Add(new EnumerableConverter());
        return list;
    }
}