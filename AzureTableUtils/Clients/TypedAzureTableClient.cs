using Azure;
using Azure.Data.Tables;
using WebGate.Azure.TableUtils.Models;

namespace WebGate.Azure.TableUtils;

/// <summary>
/// Typed CRUD wrapper around <see cref="TableClient"/> for a single POCO type <typeparamref name="T"/>.
/// Serializes nested objects, enums, decimals, arrays, and enumerables to flattened table properties.
/// </summary>
/// <typeparam name="T">Entity type stored in the bound table.</typeparam>
/// <remarks>
/// <para>
/// Preferred surface in 10.x: <see cref="InsertOrReplaceAsync"/>, <see cref="InsertOrMergeAsync"/>,
/// <see cref="GetByIdAsync(string, string)"/>, <see cref="GetAllAsync()"/> / <see cref="GetAllAsync(string)"/>,
/// and custom OData filters via <see cref="QueryAsync(string?, CancellationToken)"/> / <see cref="TableFilter"/>.
/// Use <see cref="TableClient"/> for deletes and advanced SDK operations.
/// </para>
/// <para>
/// Create via <see cref="ExtendedAzureTableClientService"/> or pass an existing <see cref="TableClient"/>.
/// Row key and partition key are always supplied by the caller (except <see cref="GetByIdAsync(string)"/>).
/// </para>
/// </remarks>
public class TypedAzureTableClient<T> where T : class
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
    [Obsolete("Use TableClient instead.")]
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
        return await QueryAndMapAsync(null, options: null, cancellationToken: default);
    }

    /// <summary>
    /// Returns all entities with the given partition key, deserialized as <typeparamref name="T"/>.
    /// </summary>
    /// <param name="partitionKey">Partition key filter. Single quotes are OData-escaped.</param>
    /// <returns>Matching rows; empty list if none match.</returns>
    public async Task<List<TableEntityResult<T>>> GetAllAsync(string partitionKey)
    {
        return await QueryAndMapAsync(TableFilter.PartitionKeyEquals(partitionKey), options: null, cancellationToken: default);
    }

    /// <summary>
    /// Returns entities matching an OData filter, deserialized as <typeparamref name="T"/>.
    /// </summary>
    /// <param name="filter">
    /// OData filter as accepted by <see cref="TableClient.QueryAsync{T}(string, int?, IEnumerable{string}, CancellationToken)"/>.
    /// Build with <see cref="TableFilter"/>. Pass <see langword="null"/> or empty to return all entities.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching rows; empty list if none match.</returns>
    public async Task<List<TableEntityResult<T>>> QueryAsync(string? filter, CancellationToken cancellationToken = default)
    {
        return await QueryAndMapAsync(filter, options: null, cancellationToken);
    }

    /// <summary>
    /// Returns entities matching an OData filter with optional paging / early-stop limits.
    /// </summary>
    /// <param name="filter">OData filter built with <see cref="TableFilter"/>.</param>
    /// <param name="options">Optional <see cref="TableQueryOptions.MaxEntities"/> / <see cref="TableQueryOptions.MaxPerPage"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<List<TableEntityResult<T>>> QueryAsync(
        string? filter,
        TableQueryOptions? options,
        CancellationToken cancellationToken = default)
    {
        return await QueryAndMapAsync(filter, options, cancellationToken);
    }

    /// <summary>
    /// Streams entities matching an OData filter with optional paging / early-stop limits.
    /// </summary>
    /// <param name="filter">OData filter from <see cref="TableFilter"/>; null/empty reads all rows (use with care).</param>
    /// <param name="options">Optional <see cref="TableQueryOptions.MaxEntities"/> / <see cref="TableQueryOptions.MaxPerPage"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async IAsyncEnumerable<TableEntityResult<T>> EnumerateAsync(
        string? filter,
        TableQueryOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? odataFilter = string.IsNullOrEmpty(filter) ? null : filter;
        AsyncPageable<TableEntity> resultItems = _tableClient.QueryAsync<TableEntity>(
            odataFilter,
            maxPerPage: options?.MaxPerPage,
            cancellationToken: cancellationToken);

        var count = 0;
        await foreach (var item in resultItems.WithCancellation(cancellationToken))
        {
            yield return TableEntityResult<T>.BuildTableEntityResult<T>(item);
            count++;
            if (options?.MaxEntities is int maxEntities && count >= maxEntities)
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// Returns deserialized entities matching an OData filter (convenience over query results + <c>Entity</c>).
    /// </summary>
    /// <param name="filter">OData filter from <see cref="TableFilter"/>.</param>
    /// <param name="options">Optional early-stop / page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<List<T>> QueryEntitiesAsync(
        string? filter,
        TableQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync(filter, options, cancellationToken);
        return rows.Select(entry => entry.Entity).ToList();
    }

    /// <summary>
    /// Returns up to <paramref name="take"/> entities in a partition using table row-key order
    /// (use <see cref="TableRowKeys"/> when newest-first by time is required without a property scan).
    /// </summary>
    /// <param name="partitionKey">Partition to scan.</param>
    /// <param name="take">Maximum rows (row-key order).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<List<T>> QueryPartitionTakeAsync(
        string partitionKey,
        int take,
        CancellationToken cancellationToken = default)
    {
        var filter = TableFilter.PartitionKeyEquals(partitionKey);
        var options = new TableQueryOptions { MaxEntities = take, MaxPerPage = take };
        return QueryEntitiesAsync(filter, options, cancellationToken);
    }

    /// <summary>
    /// Top-N in a partition by a numeric property (full partition scan, bounded memory via <see cref="TableQueryTopSelect"/>).
    /// </summary>
    /// <param name="partitionKey">Partition to scan.</param>
    /// <param name="orderKeySelector">Numeric key per entity (for example a tick column).</param>
    /// <param name="take">Number of rows to return after sorting.</param>
    /// <param name="descending">When <see langword="true"/>, highest keys first.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<List<T>> QueryTopByInPartitionAsync(
        string partitionKey,
        Func<T, long> orderKeySelector,
        int take,
        bool descending = true,
        CancellationToken cancellationToken = default)
    {
        return TableQueryTopSelect.SelectTopEntitiesByAsync(
            this,
            TableFilter.PartitionKeyEquals(partitionKey),
            orderKeySelector,
            take,
            descending,
            cancellationToken);
    }

    /// <summary>
    /// Returns entities matching an OData filter, deserialized as <typeparamref name="T"/>.
    /// </summary>
    /// <param name="query">
    /// OData filter as accepted by <see cref="TableClient.QueryAsync{T}(string, int?, IEnumerable{string}, CancellationToken)"/>.
    /// Pass <see langword="null"/> to return all entities.
    /// </param>
    /// <returns>Matching rows; empty list if none match.</returns>
    [Obsolete("Use QueryAsync(filter) for custom OData filters, or GetAllAsync / GetAllAsync(partitionKey).")]
    public async Task<List<TableEntityResult<T>>> GetAllByQueryAsync(string? query)
    {
        return await QueryAsync(query);
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
    /// <remarks>
    /// <para>
    /// Obsolete. Migrate to <c>TableClient.DeleteEntityAsync(partitionKey, rowKey)</c>.
    /// </para>
    /// <para>
    /// <b>Parameter order differs:</b> this method is <c>(rowKey, partitionKey)</c>;
    /// <see cref="TableClient"/> expects <c>(partitionKey, rowKey)</c>.
    /// </para>
    /// <code>
    /// // old (this API):
    /// await client.DeleteEntityAsync(rowKey, partitionKey);
    /// // new (SDK):
    /// await client.TableClient.DeleteEntityAsync(partitionKey, rowKey);
    /// </code>
    /// </remarks>
    [Obsolete(
        "Use TableClient.DeleteEntityAsync(partitionKey, rowKey). " +
        "Parameter order is reversed: this method is (rowKey, partitionKey), the SDK is (partitionKey, rowKey).",
        error: true)]
    public async Task<Response> DeleteEntityAsync(string rowKey, string partitionKey)
    {
        return await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
    }

    private async Task<List<TableEntityResult<T>>> QueryAndMapAsync(
        string? query,
        TableQueryOptions? options,
        CancellationToken cancellationToken = default)
    {
        List<TableEntityResult<T>> items = [];
        await foreach (var item in EnumerateAsync(query, options, cancellationToken))
        {
            items.Add(item);
        }

        return items;
    }
}
