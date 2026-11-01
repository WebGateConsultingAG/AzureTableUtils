using System.Globalization;
using Azure.Data.Tables;

namespace WebGate.Azure.TableUtils;

/// <summary>
/// Builds OData filter strings for Azure Tables <c>QueryAsync</c> (string filter overload).
/// Property names are emitted as identifiers; values are formatted as Azure Tables literals.
/// </summary>
/// <remarks>
/// Use with <see cref="TypedAzureTableClient{T}.QueryAsync(string?, CancellationToken)"/> or
/// <see cref="MultiEntityAzureTableClient.QueryAsync"/>. Dates take <see cref="DateTimeOffset"/>;
/// convert <see cref="DateTime"/> with <see cref="TableDateTime.EnsureUtc(DateTime)"/> first.
/// Nested TableUtils columns use flattened names (<c>Parent_Child_Id</c>).
/// </remarks>
public static class TableFilter
{
    private const string DATETIME_FORMAT = "yyyy-MM-ddTHH:mm:ss.fffffffZ";

    /// <summary>Filter: <c>PartitionKey eq '{partitionKey}'</c> (OData-escaped).</summary>
    public static string PartitionKeyEquals(string partitionKey)
    {
        return Equal(nameof(ITableEntity.PartitionKey), partitionKey);
    }

    /// <summary>Filter: <c>RowKey eq '{rowKey}'</c> (OData-escaped).</summary>
    public static string RowKeyEquals(string rowKey)
    {
        return Equal(nameof(ITableEntity.RowKey), rowKey);
    }

    /// <summary>Equality filter for one property.</summary>
    public static string Equal(string propertyName, object? value) =>
        Compare(propertyName, QueryComparisons.EQUAL, value);

    /// <summary>Inequality filter for one property.</summary>
    public static string NotEqual(string propertyName, object? value) =>
        Compare(propertyName, QueryComparisons.NOT_EQUAL, value);

    /// <summary>Greater-than filter for one property.</summary>
    public static string GreaterThan(string propertyName, object? value) =>
        Compare(propertyName, QueryComparisons.GREATER_THAN, value);

    /// <summary>Greater-than-or-equal filter for one property.</summary>
    public static string GreaterThanOrEqual(string propertyName, object? value) =>
        Compare(propertyName, QueryComparisons.GREATER_THAN_OR_EQUAL, value);

    /// <summary>Less-than filter for one property.</summary>
    public static string LessThan(string propertyName, object? value) =>
        Compare(propertyName, QueryComparisons.LESS_THAN, value);

    /// <summary>Less-than-or-equal filter for one property.</summary>
    public static string LessThanOrEqual(string propertyName, object? value) =>
        Compare(propertyName, QueryComparisons.LESS_THAN_OR_EQUAL, value);

    /// <summary>
    /// Builds <c>{propertyName} {comparison} {literal}</c> with Azure Tables literal formatting.
    /// </summary>
    /// <param name="propertyName">Column or flattened property name (not OData-escaped as identifier).</param>
    /// <param name="comparison">One of <see cref="QueryComparisons"/>.</param>
    /// <param name="value">Value formatted by <see cref="FormatLiteral"/> rules.</param>
    public static string Compare(string propertyName, string comparison, object? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        ArgumentException.ThrowIfNullOrEmpty(comparison);
        return $"{propertyName} {comparison} {FormatLiteral(value)}";
    }

    /// <summary>Alias for <see cref="Compare"/>.</summary>
    public static string FromValue(string propertyName, string comparison, object? value) =>
        Compare(propertyName, comparison, value);

    /// <summary>Combines filters with logical <c>and</c>.</summary>
    public static string And(params string[] filters)
    {
        return Combine(TableLogicalOperators.AND, filters);
    }

    /// <summary>Combines filters with logical <c>or</c>.</summary>
    public static string Or(params string[] filters)
    {
        return Combine(TableLogicalOperators.OR, filters);
    }

    /// <summary>
    /// Combines non-empty filter fragments with <paramref name="operatorType"/> (typically <see cref="TableLogicalOperators.AND"/> or <see cref="TableLogicalOperators.OR"/>).
    /// </summary>
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

    /// <summary>
    /// Builds one OData filter from structured conditions and a logical operator.
    /// </summary>
    /// <param name="conditions">Clauses combined with <paramref name="operatorType"/>.</param>
    /// <param name="operatorType">Logical operator; defaults to <see cref="TableLogicalOperators.AND"/>.</param>
    public static string CombineConditions(
        IEnumerable<TableFilterCondition> conditions,
        string operatorType = TableLogicalOperators.AND)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentException.ThrowIfNullOrEmpty(operatorType);

        var parts = conditions
            .Select(condition => FromValue(condition.PropertyName, condition.Comparison, condition.Value))
            .ToArray();

        return Combine(operatorType, parts);
    }

    /// <summary>Escapes single quotes in OData string literals (<c>'</c> → <c>''</c>).</summary>
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
