using Azure;
using Azure.Data.Tables;

namespace WebGate.Azure.TableUtils.Models;

/// <summary>
/// Azure Table row metadata plus a deserialized POCO from <see cref="ObjectBuilder"/>.
/// </summary>
/// <typeparam name="T">Business entity type (or <see cref="object"/> for multi-entity queries).</typeparam>
/// <remarks>
/// Returned by <see cref="TypedAzureTableClient{T}"/> and <see cref="MultiEntityAzureTableClient"/> query/get APIs.
/// Factory methods map a raw <see cref="TableEntity"/> without an extra round trip.
/// </remarks>
public class TableEntityResult<T>(ITableEntity tableEntity, T entity)
{
    /// <summary>Row key from storage.</summary>
    public string RowKey { get; set; } = tableEntity.RowKey;

    /// <summary>Partition key from storage.</summary>
    public string PartitionKey { set; get; } = tableEntity.PartitionKey;

    /// <summary>Concurrency token from storage.</summary>
    public ETag ETag { get; set; } = tableEntity.ETag;

    /// <summary>Server-maintained timestamp when present.</summary>
    public DateTimeOffset? Timestamp { get; set; } = tableEntity.Timestamp;

    /// <summary>Deserialized business object.</summary>
    public T Entity { get; set; } = entity;

    /// <summary>
    /// Builds a result for a single registered POCO type.
    /// </summary>
    /// <typeparam name="TCreate">Target POCO type.</typeparam>
    /// <param name="tableEntity">Raw table row.</param>
    public static TableEntityResult<TCreate> BuildTableEntityResult<TCreate>(TableEntity tableEntity)
    {
        var businessEntity = ObjectBuilder.Build<TCreate>(tableEntity);
        return new TableEntityResult<TCreate>(tableEntity, businessEntity);
    }

    /// <summary>
    /// Builds a result when the CLR type is chosen at runtime (multi-entity table).
    /// </summary>
    /// <param name="typeC">Concrete entity type to deserialize.</param>
    /// <param name="tableEntity">Raw table row.</param>
    public static TableEntityResult<object> BuildTableEntityResultWithType(Type typeC, TableEntity tableEntity)
    {
        var businessEntity = ObjectBuilder.BuildByType(typeC, tableEntity);
        return new TableEntityResult<object>(tableEntity, businessEntity);
    }
}
