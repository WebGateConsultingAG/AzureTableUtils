using System.Globalization;

namespace WebGate.Azure.TableUtils;

/// <summary>
/// Builds OData filter strings for Azure Tables <c>QueryAsync</c> (string filter overload).
/// Property names are emitted as identifiers; values are formatted as Azure Tables literals.
/// </summary>
/// <remarks>
/// Use with <see cref="TypedAzureTableClient{T}.QueryAsync"/> or
/// <see cref="MultiEntityAzureTableClient.QueryAsync"/>. Dates take <see cref="DateTimeOffset"/>;
/// convert <see cref="DateTime"/> with <see cref="TableDateTime.EnsureUtc(DateTime)"/> first.
/// Nested TableUtils columns use flattened names (<c>Parent_Child_Id</c>).
/// </remarks>
public static class TableFilter
{
    private const string DATETIME_FORMAT = "yyyy-MM-ddTHH:mm:ss.fffffffZ";

    public static string PartitionKeyEquals(string partitionKey)
    {
        return Equal(nameof(ITableEntity.PartitionKey), partitionKey);
    }

    public static string RowKeyEquals(string rowKey)
    {
        return Equal(nameof(ITableEntity.RowKey), rowKey);
    }

    public static string Equal(string propertyName, object? value) =>
        Compare(propertyName, QueryComparisons.EQUAL, value);

    public static string NotEqual(string propertyName, object? value) =>
        Compare(propertyName, QueryComparisons.NOT_EQUAL, value);

    public static string GreaterThan(string propertyName, object? value) =>
        Compare(propertyName, QueryComparisons.GREATER_THAN, value);

    public static string GreaterThanOrEqual(string propertyName, object? value) =>
        Compare(propertyName, QueryComparisons.GREATER_THAN_OR_EQUAL, value);

    public static string LessThan(string propertyName, object? value) =>
        Compare(propertyName, QueryComparisons.LESS_THAN, value);

    public static string LessThanOrEqual(string propertyName, object? value) =>
        Compare(propertyName, QueryComparisons.LESS_THAN_OR_EQUAL, value);

    public static string Compare(string propertyName, string comparison, object? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        ArgumentException.ThrowIfNullOrEmpty(comparison);
        return $"{propertyName} {comparison} {FormatLiteral(value)}";
    }

    public static string FromValue(string propertyName, string comparison, object? value) =>
        Compare(propertyName, comparison, value);

    public static string And(params string[] filters)
    {
        return Combine(TableOperators.AND, filters);
    }

    public static string Or(params string[] filters)
    {
        return Combine(TableOperators.OR, filters);
    }

    public static string Combine(string operatorType, params string?[] filters)
    {
        ArgumentException.ThrowIfNullOrEmpty(operatorType);
        ArgumentNullException.ThrowIfNull(filters);

        string? combined = null;
        foreach (string? filter in filters)
        {
            combined = Append(combined, operatorType, filter);
        }

        return combined ?? string.Empty;
    }

    public static string Escape(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }

    private static string FormatLiteral(object? value)
    {
        return value switch
        {
            null => $"'{string.Empty}'",
            string stringValue => $"'{Escape(stringValue)}'",
            int intValue => intValue.ToString(CultureInfo.InvariantCulture),
            long longValue => $"{longValue}L",
            bool boolValue => boolValue.ToString().ToLowerInvariant(),
            double doubleValue => doubleValue.ToString(CultureInfo.InvariantCulture),
            Guid guidValue => $"guid'{guidValue:D}'",
            DateTimeOffset dateTimeOffsetValue =>
                $"datetime'{dateTimeOffsetValue.UtcDateTime.ToString(DATETIME_FORMAT, CultureInfo.InvariantCulture)}'",
            DateTime dateTimeValue => FormatLiteral(new DateTimeOffset(TableDateTime.EnsureUtc(dateTimeValue))),
            Enum enumValue => $"'{Escape(enumValue.ToString() ?? string.Empty)}'",
            _ => $"'{Escape(value.ToString() ?? string.Empty)}'",
        };
    }

    private static string Append(string? left, string operatorType, string? right)
    {
        if (string.IsNullOrEmpty(left))
        {
            return right ?? string.Empty;
        }

        if (string.IsNullOrEmpty(right))
        {
            return left;
        }

        return $"({left}) {operatorType} ({right})";
    }
}
