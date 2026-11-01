using Azure.Data.Tables;
using WebGate.Azure.TableUtils.Converter;

namespace WebGate.Azure.TableUtils;

/// <summary>
/// Reflects a POCO into flat Azure Table property names and values (see entity mapping rules in README).
/// </summary>
/// <remarks>
/// Used by typed clients on upsert. Nested objects flatten with <c>_</c>; null properties are omitted.
/// <see cref="DateTime"/> values are normalized to UTC before write. Unsupported shapes fall through to
/// nested recursion or <see cref="ConverterFactory"/> converters.
/// </remarks>
public static class ObjectSerializer
{
    /// <summary>
    /// Serializes <paramref name="obj"/> to a property dictionary suitable for <see cref="TableEntity"/>.
    /// </summary>
    /// <param name="obj">Root object (typically the POCO instance being stored).</param>
    /// <returns>Column name → value map (does not include partition/row keys).</returns>
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
                entities.Add(EntityMapping.BuildEntityName(path, id), NormalizeTableValue(value));
            }
            else
            {
                ProcessObject(value, EntityMapping.BuildEntityName(path, id), entities);
            }
        }
    }

    private static object NormalizeTableValue(object value)
    {
        if (value is DateTime dateTime)
        {
            return dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime,
                DateTimeKind.Local => dateTime.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
            };
        }

        return value;
    }
}
