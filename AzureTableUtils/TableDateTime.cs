namespace WebGate.Azure.TableUtils;

/// <summary>
/// Normalizes <see cref="DateTime"/> values before they are written to Azure Tables.
/// <see cref="TableClient"/> rejects <see cref="DateTimeKind.Unspecified"/> on persist.
/// </summary>
/// <remarks>
/// Unspecified kind is treated as already-UTC ticks (no offset conversion).
/// Use this on write paths; <see cref="TableFilter"/> takes <see cref="DateTimeOffset"/> only.
/// </remarks>
public static class TableDateTime
{
    public static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    public static DateTime? EnsureUtc(DateTime? value)
    {
        return value.HasValue ? EnsureUtc(value.Value) : null;
    }
}
