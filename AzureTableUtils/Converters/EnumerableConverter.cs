using Newtonsoft.Json;

namespace WebGate.Azure.TableUtils.Converter;

public class EnumerableConverter : IConverter
{
    public bool IsType(Type type)
    {
        return type.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)) && type.Name != "Byte[]" && type.Name != "String";
    }

    public string GetValue(Type type, object value)
    {
        return JsonConvert.SerializeObject(value);
    }

    public object? BuildValue(string? value, Type type)
    {
        if (!string.IsNullOrEmpty(value))
        {
            return JsonConvert.DeserializeObject(value, type);
        }
        return null;
    }
}