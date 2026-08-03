namespace WebGate.Azure.TableUtils.Models;

public class TableEntityResult<T>(ITableEntity tableEntity, T entity)
{
    public string RowKey { get; set; } = tableEntity.RowKey;
    public string PartitionKey { set; get; } = tableEntity.PartitionKey;
    public ETag ETag { get; set; } = tableEntity.ETag;
    public DateTimeOffset? Timestamp { get; set; } = tableEntity.Timestamp;
    public T Entity { get; set; } = entity;

    public static TableEntityResult<TCreate> BuildTableEntityResult<TCreate>(TableEntity tableEntity)
    {
        var businessEntity = ObjectBuilder.Build<TCreate>(tableEntity);
        return new TableEntityResult<TCreate>(tableEntity, businessEntity);
    }

    public static TableEntityResult<object> BuildTableEntityResultWithType(Type typeC, TableEntity tableEntity)
    {
        var businessEntity = ObjectBuilder.BuildByType(typeC, tableEntity);
        return new TableEntityResult<object>(tableEntity, businessEntity);
    }
}