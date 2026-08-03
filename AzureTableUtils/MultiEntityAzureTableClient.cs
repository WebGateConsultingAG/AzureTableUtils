using WebGate.Azure.TableUtils.Models;

namespace WebGate.Azure.TableUtils;

public class MultiEntityAzureTableClient
{
    private readonly TableClient _tableClient;
    private readonly Dictionary<Type, string> _typeRegistry = new();

    public MultiEntityAzureTableClient(TableClient tableClient)
    {
        _tableClient = tableClient;
    }

    public TableClient TableClient => _tableClient;

    public TableClient GetTableClient() => TableClient;

    public void RegisterType<T>()
    {
        RegisterType<T>(typeof(T).Name);
    }

    public void RegisterType<T>(string typePrefix)
    {
        _typeRegistry.Add(typeof(T), typePrefix);
    }

    public async Task<List<TableEntityResult<object>>> GetAllAsync()
    {
        return await GetAllByQueryAsync(null);
    }

    public async Task<List<TableEntityResult<object>>> GetAllAsync(string partitionKey)
    {
        return await GetAllByQueryAsync(ODataFilter.PartitionKeyEquals(partitionKey));
    }

    public async Task<List<TableEntityResult<object>>> GetAllByQueryAsync(string? query)
    {
        AsyncPageable<TableEntity> resultItems = _tableClient.QueryAsync<TableEntity>(query);

        List<TableEntityResult<object>> items = new();
        await foreach (var item in resultItems)
        {
            Type? entityType = _typeRegistry.Where(kvp => item.RowKey.StartsWith(kvp.Value + "_")).Select(kvp => kvp.Key).FirstOrDefault();
            if (entityType == null)
            {
                throw new ArgumentOutOfRangeException($"No registered type found for Tableentry with ID {item.RowKey}.");
            }
            items.Add(TableEntityResult<object>.BuildTableEntityResultWithType(entityType, item));
        }
        return items;
    }

    public async Task<TableEntityResult<T>?> GetByIdAsync<T>(string rowKey, string partitionKey)
    {
        if (!_typeRegistry.TryGetValue(typeof(T), out string? prefix))
        {
            throw new ArgumentOutOfRangeException($"No registered type found for {typeof(T)}.");
        }

        NullableResponse<TableEntity> tableEntity = await _tableClient.GetEntityIfExistsAsync<TableEntity>(partitionKey, prefix + "_" + rowKey);
        if (tableEntity.HasValue)
        {
            return TableEntityResult<T>.BuildTableEntityResult<T>(tableEntity.Value!);
        }
        return null;
    }

    public async Task<Response> InsertOrReplaceAsync<T>(string rowKey, string partitionKey, T obj)
    {
        return await UpsertAsync(rowKey, partitionKey, obj, TableUpdateMode.Replace);
    }

    public async Task<Response> InsertOrMergeAsync<T>(string rowKey, string partitionKey, T obj)
    {
        return await UpsertAsync(rowKey, partitionKey, obj, TableUpdateMode.Merge);
    }

    public async Task<Response> DeleteEntityByTypeAsync<T>(string rowKey, string partitionKey)
    {
        if (!_typeRegistry.TryGetValue(typeof(T), out string? prefix))
        {
            throw new ArgumentOutOfRangeException($"No registered type found for {typeof(T)}.");
        }
        return await _tableClient.DeleteEntityAsync(partitionKey, prefix + "_" + rowKey);
    }

    public async Task<Response> DeleteEntityAsync(string completeRowKey, string partitionKey)
    {
        return await _tableClient.DeleteEntityAsync(partitionKey, completeRowKey);
    }

    private async Task<Response> UpsertAsync<T>(string rowKey, string partitionKey, T obj, TableUpdateMode updateMode)
    {
        ArgumentNullException.ThrowIfNull(obj);
        Type entityType = obj.GetType();
        if (!_typeRegistry.TryGetValue(entityType, out string? prefix))
        {
            throw new ArgumentOutOfRangeException($"No registered type found for {entityType}.");
        }

        var properties = ObjectSerializer.Serialize(obj);
        TableEntity tableEntity = new(properties)
        {
            RowKey = prefix + "_" + rowKey,
            PartitionKey = partitionKey
        };
        return await _tableClient.UpsertEntityAsync(tableEntity, updateMode);
    }
}
