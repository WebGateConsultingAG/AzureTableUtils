using Newtonsoft.Json;

namespace WebGate.Azure.TableUtils.Converter;

public class ArrayConverter : IConverter
{
    public bool IsType(Type type)
    {
        return type.IsArray && type.Name != "Byte[]";
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