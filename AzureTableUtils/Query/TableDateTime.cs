using Azure.Data.Tables;

namespace WebGate.Azure.TableUtils;

/// <summary>
/// Normalizes <see cref="DateTime"/> values before they are written to Azure Tables.
/// The Azure Tables SDK rejects <see cref="DateTimeKind.Unspecified"/> on persist.
/// </summary>
/// <remarks>
/// Unspecified kind is treated as already-UTC ticks (no offset conversion).
/// Use this on write paths; <see cref="TableFilter"/> takes <see cref="DateTimeOffset"/> only.
/// </remarks>
public static class TableDateTime
{
    /// <summary>
    /// Returns <paramref name="value"/> as UTC for table writes and filter literals.
    /// </summary>
    /// <param name="value">Input instant; unspecified kind is treated as UTC ticks without offset shift.</param>
    public static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    /// <summary>Nullable overload of <see cref="EnsureUtc(DateTime)"/>.</summary>
    public static DateTime? EnsureUtc(DateTime? value)
    {
        return value.HasValue ? EnsureUtc(value.Value) : null;
    }
}
