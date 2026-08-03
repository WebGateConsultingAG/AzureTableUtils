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
        obj.GetType().GetProperties().Where(propertyInfo => propertyInfo.CanRead && propertyInfo.CanWrite).ToList().ForEach(propertyInfo =>
        {
            string id = propertyInfo.Name;
            string entityName = BuildEntityName(path, id);
            if (tableEntity.TryGetValue(entityName, out var value))
            {
                Type pType = propertyInfo.PropertyType;
                IConverter? converter = ConverterFactory.FindConverter(pType);
                if (converter != null)
                {
                    propertyInfo.SetValue(obj, converter.BuildValue(value != null ? value.ToString() : null, pType), index: null);
                }
                else
                {
                    if (value.GetType().FullName == "System.DateTimeOffset" && IsDateTime(pType))
                    {
                        DateTimeOffset dtoValue = (DateTimeOffset)value;
                        propertyInfo.SetValue(obj, DateTime.SpecifyKind(dtoValue.DateTime, DateTimeKind.Utc), index: null);
                        return;
                    }
                    if (pType.IsValueType || pType.Name == "Byte[]" || pType.Name == "String")
                    {
                        propertyInfo.SetValue(obj, value, index: null);
                    }
                    else
                    {
                        object child = RuntimeHelpers.GetUninitializedObject(pType);
                        ProcessObject(child, id, tableEntity);
                    }
                }
            }
            else
            {
                Type pType = propertyInfo.PropertyType;
                if (!pType.IsValueType && pType.Name != "Byte[]" || pType.Name != "String")
                {
                    if (HasChildObjectInformation(id, tableEntity))
                    {
                        object child = RuntimeHelpers.GetUninitializedObject(pType);
                        ProcessObject(child, id, tableEntity);
                        propertyInfo.SetValue(obj, child, index: null);
                    }
                }
            }
        });
    }

    private static bool HasChildObjectInformation(string id, TableEntity tableEntity)
    {
        return tableEntity.Where(p => p.Key.StartsWith(id + "_") && p.Value != null).Count() > 0;
    }

    private static string BuildEntityName(string? path, string id)
    {
        if (string.IsNullOrEmpty(path))
        {
            return id;
        }
        return path + "_" + id;
    }
    private static bool IsDateTime(Type pType)
    {
        return pType.FullName == "System.DateTime" || Nullable.GetUnderlyingType(pType) == typeof(DateTime);
    }
}