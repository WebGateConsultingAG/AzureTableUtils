namespace WebGate.Azure.TableUtils;

/// <summary>
/// OData comparison operators for Azure Tables filters (used with <see cref="TableFilter.Compare"/>).
/// </summary>
/// <remarks>
/// Values match the Azure Tables OData subset (<c>eq</c>, <c>ne</c>, …). Prefer the named helpers on
/// <see cref="TableFilter"/> (<see cref="TableFilter.Equal"/>, …) instead of passing raw strings.
/// </remarks>
public static class QueryComparisons
{
    /// <summary>Equality (<c>eq</c>).</summary>
    public const string EQUAL = "eq";

    /// <summary>Inequality (<c>ne</c>).</summary>
    public const string NOT_EQUAL = "ne";

    /// <summary>Greater than (<c>gt</c>).</summary>
    public const string GREATER_THAN = "gt";

    /// <summary>Greater than or equal (<c>ge</c>).</summary>
    public const string GREATER_THAN_OR_EQUAL = "ge";

    /// <summary>Less than (<c>lt</c>).</summary>
    public const string LESS_THAN = "lt";

    /// <summary>Less than or equal (<c>le</c>).</summary>
    public const string LESS_THAN_OR_EQUAL = "le";
}
