namespace WebGate.Azure.TableUtils;

/// <summary>
/// Optional limits for <see cref="TypedAzureTableClient{T}.QueryAsync(string?, TableQueryOptions?, CancellationToken)"/>.
/// </summary>
public sealed class TableQueryOptions
{
    /// <summary>
    /// Stops reading after this many entities (use with time-ordered row keys for «newest first» within a partition).
    /// </summary>
    public int? MaxEntities { get; init; }

    /// <summary>
    /// Page size passed to the underlying Azure Tables <c>TableClient.QueryAsync</c> call.
    /// </summary>
    public int? MaxPerPage { get; init; }
}
