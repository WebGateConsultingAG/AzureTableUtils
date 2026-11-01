namespace WebGate.Azure.TableUtils;

/// <summary>
/// OData logical operators for <see cref="TableFilter.Combine"/>.
/// </summary>
public static class TableLogicalOperators
{
    /// <summary>Logical AND between filter clauses.</summary>
    public const string AND = "and";

    /// <summary>Logical OR between filter clauses.</summary>
    public const string OR = "or";
}
