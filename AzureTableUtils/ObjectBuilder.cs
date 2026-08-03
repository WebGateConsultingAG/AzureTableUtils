using System.Runtime.CompilerServices;
using WebGate.Azure.TableUtils.Converter;

namespace WebGate.Azure.TableUtils;

public static class ObjectBuilder
{
    public static T Build<T>(TableEntity tableEntity)
    {
        T result = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        ProcessObject(result, null, tableEntity);
        return result;
    }

    public static object BuildByType(Type typeT, TableEntity tableEntity)
    {
        object result = RuntimeHelpers.GetUninitializedObject(typeT);
        ProcessObject(result, null, tableEntity);
        return result;
    }

    private static void ProcessObject(object obj, string? path, TableEntity tableEntity)
    {
        foreach (var propertyInfo in EntityMapping.GetWritableProperties(obj.GetType()))
        {
            string id = propertyInfo.Name;
            string entityName = EntityMapping.BuildEntityName(path, id);
            Type pType = propertyInfo.PropertyType;

            if (tableEntity.TryGetValue(entityName, out var value))
            {
                IConverter? converter = ConverterFactory.FindConverter(pType);
                if (converter != null)
                {
                    propertyInfo.SetValue(obj, converter.BuildValue(value?.ToString(), pType));
                    continue;
                }

                if (value is DateTimeOffset dtoValue && IsDateTime(pType))
                {
                    propertyInfo.SetValue(obj, DateTime.SpecifyKind(dtoValue.DateTime, DateTimeKind.Utc));
                    continue;
                }

                if (EntityMapping.IsPrimitiveTableType(pType))
                {
                    propertyInfo.SetValue(obj, value);
                    continue;
                }

                object child = RuntimeHelpers.GetUninitializedObject(pType);
                ProcessObject(child, entityName, tableEntity);
                propertyInfo.SetValue(obj, child);
            }
            else if (!EntityMapping.IsPrimitiveTableType(pType) && HasChildObjectInformation(entityName, tableEntity))
            {
                object child = RuntimeHelpers.GetUninitializedObject(pType);
                ProcessObject(child, entityName, tableEntity);
                propertyInfo.SetValue(obj, child);
            }
        }
    }

    private static bool HasChildObjectInformation(string id, TableEntity tableEntity) =>
        tableEntity.Any(p => p.Key.StartsWith(id + "_", StringComparison.Ordinal) && p.Value != null);

    private static bool IsDateTime(Type pType) =>
        pType == typeof(DateTime) || Nullable.GetUnderlyingType(pType) == typeof(DateTime);
}
