namespace WebGate.Azure.TableUtils;

/// <summary>
/// Batched OData queries (OR-combined equality) and value normalization for Azure Table URL limits.
/// </summary>
public static class TableQueryBatch
{
    /// <summary>
    /// Drops null/whitespace strings, then distinct in encounter order. Null source becomes empty.
    /// </summary>
    public static List<string> DistinctNonEmpty(IEnumerable<string>? values)
    {
        return values
            ?.Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct()
            .ToList() ?? [];
    }

    /// <summary>
    /// Runs OR-combined equality filters in batches against <paramref name="client"/>.
    /// </summary>
    /// <param name="client">Typed table client.</param>
    /// <param name="propertyName">Field name for each equality filter.</param>
    /// <param name="values">Candidate values (non-empty, distinct after <see cref="DistinctNonEmpty"/>).</param>
    /// <param name="batchSize">Maximum OR conditions per request (must be ≥ 1).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="T">Entity type to read.</typeparam>
    /// <returns>Concatenated matches across all batches; empty when no values.</returns>
    /// <remarks>
    /// Avoids oversized query URLs. Duplicate keys across batches may appear if the server returns overlaps;
    /// callers dedupe if needed. Each batch is a read-only query (idempotent).
    /// </remarks>
    public static async Task<List<T>> QueryEqualsAnyAsync<T>(
        TypedAzureTableClient<T> client,
        string propertyName,
        IEnumerable<string> values,
        int batchSize,
        CancellationToken cancellationToken = default) where T : class
    {
        if (batchSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");
        }

        var distinctValues = DistinctNonEmpty(values);
        if (distinctValues.Count == 0)
        {
            return [];
        }

        var result = new List<T>();
        foreach (var valueBatch in SplitIntoBatches(distinctValues, batchSize))
        {
            var conditions = valueBatch
                .Select(value => new TableFilterCondition(propertyName, QueryComparisons.EQUAL, value))
                .ToArray();

            var odataFilter = TableFilter.CombineConditions(conditions, TableLogicalOperators.OR);
            var batchResult = await client.QueryEntitiesAsync(odataFilter, options: null, cancellationToken);
            result.AddRange(batchResult);
        }

        return result;
    }

    private static IEnumerable<List<string>> SplitIntoBatches(IReadOnlyList<string> values, int batchSize)
    {
        for (var index = 0; index < values.Count; index += batchSize)
        {
            var count = Math.Min(batchSize, values.Count - index);
            var batch = new List<string>(count);
            for (var offset = 0; offset < count; offset++)
            {
                batch.Add(values[index + offset]);
            }

            yield return batch;
        }
    }
}
