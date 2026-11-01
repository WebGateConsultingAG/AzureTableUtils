using WebGate.Azure.TableUtils.Models;

namespace WebGate.Azure.TableUtils;

/// <summary>
/// Client-side top-N selection when Azure Tables cannot sort by arbitrary properties (no OData ORDER BY).
/// </summary>
/// <remarks>
/// Scans all entities matching the OData filter (often a partition filter). Memory is bounded by the take count;
/// CPU is O(number of matching rows). For large partitions prefer time-ordered row keys via <see cref="TableRowKeys"/>
/// and <see cref="TypedAzureTableClient{T}.QueryPartitionTakeAsync"/>.
/// </remarks>
public static class TableQueryTopSelect
{
    /// <summary>
    /// Streams all entities matching <paramref name="filter"/> and returns the top <paramref name="take"/> rows
    /// by <paramref name="orderKeySelector"/> (descending when <paramref name="descending"/> is true).
    /// </summary>
    /// <param name="tableClient">Typed client used to enumerate rows.</param>
    /// <param name="filter">OData filter (often from <see cref="TableFilter.PartitionKeyEquals"/>).</param>
    /// <param name="orderKeySelector">Numeric sort key per entity.</param>
    /// <param name="take">Maximum number of rows to return.</param>
    /// <param name="descending">When true, highest keys win.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<List<TableEntityResult<T>>> SelectTopByAsync<T>(
        TypedAzureTableClient<T> tableClient,
        string? filter,
        Func<T, long> orderKeySelector,
        int take,
        bool descending = true,
        CancellationToken cancellationToken = default) where T : class
    {
        if (take <= 0)
        {
            return [];
        }

        var top = new List<(long Key, TableEntityResult<T> Row)>();

        await foreach (var row in tableClient.EnumerateAsync(filter, options: null, cancellationToken))
        {
            var key = orderKeySelector(row.Entity);
            InsertTop(top, key, row, take, descending);
        }

        top.Sort((left, right) => descending
            ? right.Key.CompareTo(left.Key)
            : left.Key.CompareTo(right.Key));

        return top.Select(entry => entry.Row).ToList();
    }

    /// <summary>
    /// Same as <see cref="SelectTopByAsync{T}"/> but returns deserialized entities only.
    /// </summary>
    public static async Task<List<T>> SelectTopEntitiesByAsync<T>(
        TypedAzureTableClient<T> tableClient,
        string? filter,
        Func<T, long> orderKeySelector,
        int take,
        bool descending = true,
        CancellationToken cancellationToken = default) where T : class
    {
        var rows = await SelectTopByAsync(
            tableClient,
            filter,
            orderKeySelector,
            take,
            descending,
            cancellationToken);
        return rows.Select(entry => entry.Entity).ToList();
    }

    private static void InsertTop<T>(
        List<(long Key, TableEntityResult<T> Row)> top,
        long key,
        TableEntityResult<T> row,
        int take,
        bool descending)
    {
        if (top.Count < take)
        {
            top.Add((key, row));
            return;
        }

        var worstIndex = 0;
        var worstKey = top[0].Key;
        for (var index = 1; index < top.Count; index++)
        {
            var candidate = top[index].Key;
            var isWorse = descending ? candidate < worstKey : candidate > worstKey;
            if (isWorse)
            {
                worstKey = candidate;
                worstIndex = index;
            }
        }

        var replacesWorst = descending ? key > worstKey : key < worstKey;
        if (replacesWorst)
        {
            top[worstIndex] = (key, row);
        }
    }
}
