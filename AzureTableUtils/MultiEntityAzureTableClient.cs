using WebGate.Azure.TableUtils.Models;

namespace WebGate.Azure.TableUtils;

/// <summary>
/// CRUD wrapper around <see cref="TableClient"/> that stores multiple POCO types in one table.
/// Each registered type gets a row-key prefix (<c>{prefix}_{rowKey}</c>).
/// </summary>
/// <remarks>
/// Register every type with <see cref="RegisterType{T}()"/> or <see cref="RegisterType{T}(string)"/> before insert or typed get/delete.
/// On read, the prefix of each row key must match a registered type; otherwise an exception is thrown.
/// Mapping of nested objects, enums, decimals, arrays, and enumerables matches <see cref="TypedAzureTableClient{T}"/>.
/// </remarks>
public class MultiEntityAzureTableClient
{
    private readonly TableClient _tableClient;
    private readonly Dictionary<Type, string> _typeRegistry = [];

    /// <summary>
    /// Creates a client bound to the given Azure Tables <paramref name="tableClient"/>.
    /// </summary>
    /// <param name="tableClient">Underlying SDK client; must not be <see langword="null"/>.</param>
    public MultiEntityAzureTableClient(TableClient tableClient)
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
    /// Registers <typeparamref name="T"/> using <c>typeof(T).Name</c> as row-key prefix.
    /// </summary>
    /// <typeparam name="T">Entity type to store in this table.</typeparam>
    /// <exception cref="ArgumentException">Thrown if <typeparamref name="T"/> is already registered.</exception>
    public void RegisterType<T>()
    {
        RegisterType<T>(typeof(T).Name);
    }

    /// <summary>
    /// Registers <typeparamref name="T"/> with a custom row-key prefix.
    /// </summary>
    /// <typeparam name="T">Entity type to store in this table.</typeparam>
    /// <param name="typePrefix">
    /// Prefix written before the caller row key (<c>{prefix}_{rowKey}</c>).
    /// Avoid prefixes that are prefixes of each other (e.g. <c>A</c> and <c>AB</c>).
    /// </param>
    /// <exception cref="ArgumentException">Thrown if <typeparamref name="T"/> is already registered.</exception>
    public void RegisterType<T>(string typePrefix)
    {
        _typeRegistry.Add(typeof(T), typePrefix);
    }

    /// <summary>
    /// Returns all entities in the table, each deserialized to its registered CLR type
    /// and exposed as <see cref="TableEntityResult{T}.Entity"/> of type <see cref="object"/>.
    /// </summary>
    /// <returns>All rows; empty list if the table has no entities.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if a row key does not start with any registered prefix.
    /// </exception>
    public async Task<List<TableEntityResult<object>>> GetAllAsync()
    {
        return await GetAllByQueryAsync(null);
    }

    /// <summary>
    /// Returns all entities with the given partition key.
    /// </summary>
    /// <param name="partitionKey">Partition key filter. Single quotes are OData-escaped.</param>
    /// <returns>Matching rows; empty list if none match.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if a row key does not start with any registered prefix.
    /// </exception>
    public async Task<List<TableEntityResult<object>>> GetAllAsync(string partitionKey)
    {
        return await GetAllByQueryAsync(ODataFilter.PartitionKeyEquals(partitionKey));
    }

    /// <summary>
    /// Returns entities matching an OData filter.
    /// </summary>
    /// <param name="query">
    /// OData filter as accepted by <see cref="TableClient.QueryAsync{T}(string, int?, IEnumerable{string}, CancellationToken)"/>.
    /// Pass <see langword="null"/> to return all entities.
    /// </param>
    /// <returns>Matching rows; empty list if none match.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if a row key does not start with any registered prefix.
    /// </exception>
    public async Task<List<TableEntityResult<object>>> GetAllByQueryAsync(string? query)
    {
        AsyncPageable<TableEntity> resultItems = _tableClient.QueryAsync<TableEntity>(query);

        List<TableEntityResult<object>> items = [];
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

    /// <summary>
    /// Gets a single entity of type <typeparamref name="T"/> by logical row key and partition key.
    /// The stored row key is <c>{registeredPrefix}_{rowKey}</c>.
    /// </summary>
    /// <typeparam name="T">Registered entity type.</typeparam>
    /// <param name="rowKey">Logical row key without prefix.</param>
    /// <param name="partitionKey">Partition key.</param>
    /// <returns>The entity, or <see langword="null"/> if it does not exist.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <typeparamref name="T"/> is not registered.</exception>
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

    /// <summary>
    /// Inserts or replaces the entity (upsert with <see cref="TableUpdateMode.Replace"/>).
    /// Stores row key as <c>{prefix}_{rowKey}</c> using the prefix registered for the runtime type of <paramref name="obj"/>.
    /// </summary>
    /// <typeparam name="T">Compile-time type of <paramref name="obj"/>; registration uses <c>obj.GetType()</c>.</typeparam>
    /// <param name="rowKey">Logical row key without prefix.</param>
    /// <param name="partitionKey">Partition key.</param>
    /// <param name="obj">Entity to serialize; must not be <see langword="null"/> and must be registered.</param>
    /// <returns>The Azure Tables response.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the runtime type of <paramref name="obj"/> is not registered.</exception>
    public async Task<Response> InsertOrReplaceAsync<T>(string rowKey, string partitionKey, T obj)
    {
        return await UpsertAsync(rowKey, partitionKey, obj, TableUpdateMode.Replace);
    }

    /// <summary>
    /// Inserts or merges the entity (upsert with <see cref="TableUpdateMode.Merge"/>).
    /// Stores row key as <c>{prefix}_{rowKey}</c> using the prefix registered for the runtime type of <paramref name="obj"/>.
    /// </summary>
    /// <typeparam name="T">Compile-time type of <paramref name="obj"/>; registration uses <c>obj.GetType()</c>.</typeparam>
    /// <param name="rowKey">Logical row key without prefix.</param>
    /// <param name="partitionKey">Partition key.</param>
    /// <param name="obj">Entity to serialize; must not be <see langword="null"/> and must be registered.</param>
    /// <returns>The Azure Tables response.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the runtime type of <paramref name="obj"/> is not registered.</exception>
    public async Task<Response> InsertOrMergeAsync<T>(string rowKey, string partitionKey, T obj)
    {
        return await UpsertAsync(rowKey, partitionKey, obj, TableUpdateMode.Merge);
    }

    /// <summary>
    /// Deletes an entity by logical row key, building the stored key as <c>{prefix}_{rowKey}</c> for <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Registered entity type.</typeparam>
    /// <param name="rowKey">Logical row key without prefix.</param>
    /// <param name="partitionKey">Partition key.</param>
    /// <returns>The Azure Tables response.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <typeparamref name="T"/> is not registered.</exception>
    public async Task<Response> DeleteEntityByTypeAsync<T>(string rowKey, string partitionKey)
    {
        if (!_typeRegistry.TryGetValue(typeof(T), out string? prefix))
        {
            throw new ArgumentOutOfRangeException($"No registered type found for {typeof(T)}.");
        }
        return await _tableClient.DeleteEntityAsync(partitionKey, prefix + "_" + rowKey);
    }

    /// <summary>
    /// Deletes an entity by the full stored row key (including type prefix).
    /// </summary>
    /// <param name="completeRowKey">Full row key as stored in the table (e.g. from <see cref="TableEntityResult{T}.RowKey"/>).</param>
    /// <param name="partitionKey">Partition key.</param>
    /// <returns>The Azure Tables response.</returns>
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
