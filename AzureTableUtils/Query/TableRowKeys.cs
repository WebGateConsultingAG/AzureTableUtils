namespace WebGate.Azure.TableUtils;

/// <summary>
/// Row-key helpers when partition scans rely on row-key order (Azure Tables has no OData ORDER BY).
/// </summary>
public static class TableRowKeys
{
    /// <summary>
    /// Builds a row key whose ascending table order corresponds to descending UTC time
    /// (<c>{invertedTicks:D19}_{uniqueSuffix}</c>).
    /// </summary>
    /// <param name="utcTimestamp">UTC instant used for ordering (typically <see cref="DateTime.UtcNow"/>).</param>
    /// <param name="uniqueSuffix">Suffix that keeps keys unique within the same tick (for example a GUID).</param>
    /// <exception cref="ArgumentException"><paramref name="uniqueSuffix"/> is null or empty.</exception>
    public static string BuildTimeOrderedDescending(DateTime utcTimestamp, string uniqueSuffix)
    {
        ArgumentException.ThrowIfNullOrEmpty(uniqueSuffix);
        var utc = TableDateTime.EnsureUtc(utcTimestamp);
        var invertedTicks = long.MaxValue - utc.Ticks;
        return $"{invertedTicks:D19}_{uniqueSuffix}";
    }
}
