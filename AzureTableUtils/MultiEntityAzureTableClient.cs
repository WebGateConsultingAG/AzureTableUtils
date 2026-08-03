using WebGate.Azure.TableUtils.Models;

namespace WebGate.Azure.TableUtils;

public class MultiEntityAzureTableClient
{
    private readonly TableClient _tableClient;
    private readonly Dictionary<Type, string> _typeRegistry = new Dictionary<Type, string>();

    public MultiEntityAzureTableClient(TableClient tableClient)
    {
        _tableClient = tableClient;
    }

    public TableClient GetTableClient()
    { return _tableClient; }

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
        if (!_typeRegistry.ContainsKey(typeof(T)))
        {
            throw new ArgumentOutOfRangeException($"No registered type found for {typeof(T)}.");
        }
        string prefix = _typeRegistry[typeof(T)];
        NullableResponse<TableEntity> tableEntity = await _tableClient.GetEntityIfExistsAsync<TableEntity>(partitionKey, prefix + "_" + rowKey);
        if (tableEntity.HasValue)
        {
            return TableEntityResult<T>.BuildTableEntityResult<T>(tableEntity.Value!);
        }
        return null;
    }

    public async Task<Response> InsertOrReplaceAsync(string rowKey, string partitionKey, object obj)
    {
        if (!_typeRegistry.ContainsKey(obj.GetType()))
        {
            throw new ArgumentOutOfRangeException($"No registered type found for {obj.GetType()}.");
        }
        string prefix = _typeRegistry[obj.GetType()];
        var properties = ObjectSerializer.Serialize(obj);
        TableEntity tableEntity = new(properties)
        {
            RowKey = prefix + "_" + rowKey,
            PartitionKey = partitionKey
        };
        return await _tableClient.UpsertEntityAsync(tableEntity, TableUpdateMode.Replace);
    }

    public async Task<Response> InsertOrMergeAsync(string rowKey, string partitionKey, object obj)
    {
        if (!_typeRegistry.ContainsKey(obj.GetType()))
        {
            throw new ArgumentOutOfRangeException($"No registered type found for {obj.GetType()}.");
        }
        string prefix = _typeRegistry[obj.GetType()];

        var properties = ObjectSerializer.Serialize(obj);
        TableEntity tableEntity = new(properties)
        {
            RowKey = prefix + "_" + rowKey,
            PartitionKey = partitionKey
        };
        return await _tableClient.UpsertEntityAsync(tableEntity, TableUpdateMode.Merge);
    }

    public async Task<Response> DeleteEntityByTypeAsync<T>(string rowKey, string partitionKey)
    {
        if (!_typeRegistry.ContainsKey(typeof(T)))
        {
            throw new ArgumentOutOfRangeException($"No registered type found for {typeof(T)}.");
        }
        string prefix = _typeRegistry[typeof(T)];
        return await _tableClient.DeleteEntityAsync(partitionKey, prefix + "_" + rowKey);
    }

    public async Task<Response> DeleteEntityAsync(string completeRowKey, string partitionKey)
    {
        return await _tableClient.DeleteEntityAsync(partitionKey, completeRowKey);
    }
}