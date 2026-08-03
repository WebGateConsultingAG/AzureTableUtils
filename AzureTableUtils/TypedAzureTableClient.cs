using WebGate.Azure.TableUtils.Models;

namespace WebGate.Azure.TableUtils;

/// <summary>
/// Typed CRUD wrapper around <see cref="TableClient"/> for a single POCO type <typeparamref name="T"/>.
/// Serializes nested objects, enums, decimals, arrays, and enumerables to flattened table properties.
/// </summary>
/// <typeparam name="T">Entity type stored in the bound table.</typeparam>
/// <remarks>
/// Create via <see cref="ExtendedAzureTableClientService"/> or pass an existing <see cref="TableClient"/>.
/// Row key and partition key are always supplied by the caller (except <see cref="GetByIdAsync(string)"/>).
/// </remarks>
public class TypedAzureTableClient<T>
{
    private readonly TableClient _tableClient;

    /// <summary>
    /// Creates a client bound to the given Azure Tables <paramref name="tableClient"/>.
    /// </summary>
    /// <param name="tableClient">Underlying SDK client; must not be <see langword="null"/>.</param>
    public TypedAzureTableClient(TableClient tableClient)
    {
        _tableClient = tableClient;
    }

    /// <summary>
    /// Gets the underlying Azure Tables SDK client.
    /// </summary>
    public TableClient TableClient => _tableClient;

    /// <summary>
    /// Gets the underlying Azure Tables SDK client.
    /// </summary>
    /// <returns>The same instance as <see cref="TableClient"/>.</returns>
    [Obsolete("Use TableClient instead")]
    public TableClient GetTableClient()
    {
        return TableClient;
    }

    /// <summary>
    /// Returns all entities in the table, deserialized as <typeparamref name="T"/>.
    /// </summary>
    /// <returns>All matching rows; empty list if the table has no entities.</returns>
    public async Task<List<TableEntityResult<T>>> GetAllAsync()
    {
        return await GetAllByQueryAsync(null);
    }

    /// <summary>
    /// Returns all entities with the given partition key, deserialized as <typeparamref name="T"/>.
    /// </summary>
    /// <param name="partitionKey">Partition key filter. Single quotes are OData-escaped.</param>
    /// <returns>Matching rows; empty list if none match.</returns>
    public async Task<List<TableEntityResult<T>>> GetAllAsync(string partitionKey)
    {
        return await GetAllByQueryAsync(ODataFilter.PartitionKeyEquals(partitionKey));
    }

    /// <summary>
    /// Returns entities matching an OData filter, deserialized as <typeparamref name="T"/>.
    /// </summary>
    /// <param name="query">
    /// OData filter as accepted by <see cref="TableClient.QueryAsync{T}(string, int?, IEnumerable{string}, CancellationToken)"/>.
    /// Pass <see langword="null"/> to return all entities.
    /// </param>
    /// <returns>Matching rows; empty list if none match.</returns>
    public async Task<List<TableEntityResult<T>>> GetAllByQueryAsync(string? query)
    {
        AsyncPageable<TableEntity> resultItems = _tableClient.QueryAsync<TableEntity>(query);

        List<TableEntityResult<T>> items = [];
        await foreach (var item in resultItems)
        {
            items.Add(TableEntityResult<T>.BuildTableEntityResult<T>(item));
        }
        return items;
    }

    /// <summary>
    /// Gets a single entity by row key using <c>typeof(T).ToString()</c> as partition key
    /// (typically the full type name, e.g. <c>MyNamespace.MyPoco</c>).
    /// </summary>
    /// <param name="id">Row key.</param>
    /// <returns>The entity, or <see langword="null"/> if it does not exist.</returns>
    /// <remarks>
    /// Prefer <see cref="GetByIdAsync(string, string)"/> when you control the partition key explicitly.
    /// </remarks>
    public async Task<TableEntityResult<T>?> GetByIdAsync(string id)
    {
        string partitionKey = typeof(T).ToString();
        return await GetByIdAsync(id, partitionKey);
    }

    /// <summary>
    /// Gets a single entity by row key and partition key.
    /// </summary>
    /// <param name="rowKey">Row key.</param>
    /// <param name="partitionKey">Partition key.</param>
    /// <returns>The entity, or <see langword="null"/> if it does not exist.</returns>
    public async Task<TableEntityResult<T>?> GetByIdAsync(string rowKey, string partitionKey)
    {
        NullableResponse<TableEntity> tableEntity = await _tableClient.GetEntityIfExistsAsync<TableEntity>(partitionKey, rowKey);
        if (tableEntity.HasValue)
        {
            return TableEntityResult<T>.BuildTableEntityResult<T>(tableEntity.Value!);
        }
        return null;
    }

    /// <summary>
    /// Inserts or replaces the entity (upsert with <see cref="TableUpdateMode.Replace"/>).
    /// </summary>
    /// <param name="rowKey">Row key to store.</param>
    /// <param name="partitionKey">Partition key to store.</param>
    /// <param name="obj">
    /// Object to serialize. Usually an instance of <typeparamref name="T"/>, but may be another shape
    /// (e.g. a partial DTO). Existing properties not present on <paramref name="obj"/> are removed on replace.
    /// </param>
    /// <returns>The Azure Tables response.</returns>
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

    /// <summary>
    /// Inserts or merges the entity (upsert with <see cref="TableUpdateMode.Merge"/>).
    /// </summary>
    /// <param name="rowKey">Row key to store.</param>
    /// <param name="partitionKey">Partition key to store.</param>
    /// <param name="obj">
    /// Object to serialize. May be a partial DTO (not necessarily <typeparamref name="T"/>);
    /// only serialized non-null properties are written; other existing columns are kept.
    /// </param>
    /// <returns>The Azure Tables response.</returns>
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

    /// <summary>
    /// Deletes the entity identified by row key and partition key.
    /// </summary>
    /// <param name="rowKey">Row key.</param>
    /// <param name="partitionKey">Partition key.</param>
    /// <returns>The Azure Tables response.</returns>
    public async Task<Response> DeleteEntityAsync(string rowKey, string partitionKey)
    {
        return await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
    }
}
