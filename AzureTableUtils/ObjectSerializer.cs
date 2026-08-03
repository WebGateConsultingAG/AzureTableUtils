using WebGate.Azure.TableUtils.Converter;

namespace WebGate.Azure.TableUtils;

public static class ObjectSerializer
{
    public static IDictionary<string, object> Serialize(object obj)
    {
        IDictionary<string, object> entities = new Dictionary<string, object>();
        ProcessObject(obj, null, entities);
        return entities;
    }

    private static void ProcessObject(object obj, string? path, IDictionary<string, object> entities)
    {
        obj.GetType().GetProperties().Where(propertInfo => propertInfo.CanRead && propertInfo.CanWrite).ToList().ForEach(propertyInfo =>
        {
            string id = propertyInfo.Name;
            object? value = propertyInfo.GetValue(obj, index: null);
            if (value != null)
            {
                IConverter? converter = ConverterFactory.FindConverter(value.GetType());
                if (converter == null)
                {
                    if (value.GetType().IsValueType || value.GetType().Name == "Byte[]" || value.GetType().Name == "String")
                    {
                        entities.Add(BuildEntityName(path, id), value);
                    }
                    else
                    {
                        ProcessObject(value, BuildEntityName(path, id), entities);
                    }
                }
                else
                {
                    string ep = converter.GetValue(propertyInfo.GetType(), value);
                    entities.Add(BuildEntityName(path, id), ep);
                }
            }
        });
    }

    private static string BuildEntityName(string? path, string id)
    {
        if (string.IsNullOrEmpty(path))
        {
            return id;
        }
        return path + "_" + id;
    }
}