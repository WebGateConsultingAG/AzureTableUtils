using WebGate.Azure.TableUtils.Models;

namespace WebGate.Azure.TableUtils;

public class TypedAzureTableClient<T>
{
    private readonly TableClient _tableClient;

    public TypedAzureTableClient(TableClient tableClient)
    {
        _tableClient = tableClient;
    }

    public TableClient GetTableClient()
    { return _tableClient; }

    public async Task<List<TableEntityResult<T>>> GetAllAsync()
    {
        return await GetAllByQueryAsync(null);
    }

    public async Task<List<TableEntityResult<T>>> GetAllAsync(string partitionKey)
    {
        var query = $"PartitionKey eq '{partitionKey}'";
        return await GetAllByQueryAsync(query);
    }

    public async Task<List<TableEntityResult<T>>> GetAllByQueryAsync(string? query)
    {
        //$"PartitionKey eq '{partitionKey}'"
        AsyncPageable<TableEntity> resultItems = _tableClient.QueryAsync<TableEntity>(query);

        List<TableEntityResult<T>> items = new();
        await foreach (var item in resultItems)
        {
            items.Add(TableEntityResult<T>.BuildTableEntityResult<T>(item));
        }
        return items;
    }

    public async Task<TableEntityResult<T>?> GetByIdAsync(string id)
    {
        string partitionKey = typeof(T).ToString();
        return await GetByIdAsync(id, partitionKey);
    }

    public async Task<TableEntityResult<T>?> GetByIdAsync(string rowKey, string partitionKey)
    {
        NullableResponse<TableEntity> tableEntity = await _tableClient.GetEntityIfExistsAsync<TableEntity>(partitionKey, rowKey);
        if (tableEntity.HasValue)
        {
            return TableEntityResult<T>.BuildTableEntityResult<T>(tableEntity.Value!);
        }
        return null;
    }

    public async Task<Response> InsertOrReplaceAsync(string rowKey, string partitionKey, object obj)
    {
        var properties = ObjectSerializer.Serialize(obj);
        TableEntity tableEntity = new(properties)
        {
            RowKey = rowKey,
            PartitionKey = partitionKey
        };
        return await _tableClient.UpsertEntityAsync(tableEntity, TableUpdateMode.Replace);
    }

    public async Task<Response> InsertOrMergeAsync(string rowKey, string partitionKey, object obj)
    {
        var properties = ObjectSerializer.Serialize(obj);
        TableEntity tableEntity = new(properties)
        {
            RowKey = rowKey,
            PartitionKey = partitionKey
        };
        return await _tableClient.UpsertEntityAsync(tableEntity, TableUpdateMode.Merge);
    }

    public async Task<Response> DeleteEntityAsync(string rowKey, string partitionKey)
    {
        return await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
    }
}