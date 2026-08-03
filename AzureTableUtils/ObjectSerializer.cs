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
        foreach (var propertyInfo in EntityMapping.GetWritableProperties(obj.GetType()))
        {
            string id = propertyInfo.Name;
            object? value = propertyInfo.GetValue(obj);
            if (value == null)
            {
                continue;
            }

            Type valueType = value.GetType();
            IConverter? converter = ConverterFactory.FindConverter(valueType);
            if (converter != null)
            {
                entities.Add(EntityMapping.BuildEntityName(path, id), converter.GetValue(propertyInfo.PropertyType, value));
                continue;
            }

            if (EntityMapping.IsPrimitiveTableType(valueType))
            {
                entities.Add(EntityMapping.BuildEntityName(path, id), value);
            }
            else
            {
                ProcessObject(value, EntityMapping.BuildEntityName(path, id), entities);
            }
        }
    }
}
