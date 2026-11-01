using System.Runtime.CompilerServices;
using Azure.Data.Tables;
using WebGate.Azure.TableUtils.Converter;

namespace WebGate.Azure.TableUtils;

/// <summary>
/// Rehydrates a POCO from a <see cref="TableEntity"/> using the same flattening rules as <see cref="ObjectSerializer"/>.
/// </summary>
/// <remarks>
/// Instances are created uninitialized then populated property-by-property. Missing columns leave default values;
/// nested objects are reconstructed when child columns exist. Used by <see cref="Models.TableEntityResult{T}"/> factories
/// and all typed client reads.
/// </remarks>
public static class ObjectBuilder
{
    /// <summary>
    /// Deserializes <paramref name="tableEntity"/> to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">POCO type with a parameterless constructor (may be uninitialized by runtime).</typeparam>
    /// <param name="tableEntity">Stored row including dynamic properties.</param>
    public static T Build<T>(TableEntity tableEntity)
    {
        T result = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        ProcessObject(result, null, tableEntity);
        return result;
    }

    /// <summary>
    /// Deserializes <paramref name="tableEntity"/> when <paramref name="typeT"/> is known only at runtime.
    /// </summary>
    /// <param name="typeT">Concrete POCO type.</param>
    /// <param name="tableEntity">Stored row.</param>
    /// <returns>Instance of <paramref name="typeT"/>.</returns>
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

    private static bool HasChildObjectInformation(string id, TableEntity tableEntity)
    {
        return tableEntity.Any(p => p.Key.StartsWith(id + "_", StringComparison.Ordinal) && p.Value != null);
    }

    private static bool IsDateTime(Type pType)
    {
        return pType == typeof(DateTime) || Nullable.GetUnderlyingType(pType) == typeof(DateTime);
    }
}
