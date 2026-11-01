namespace WebGate.Azure.TableUtils;

/// <summary>
/// One OData filter clause for <see cref="TableFilter.CombineConditions"/>.
/// </summary>
/// <param name="PropertyName">Table property or flattened column name.</param>
/// <param name="Comparison">Operator from <see cref="QueryComparisons"/>.</param>
/// <param name="Value">Literal value formatted by <see cref="TableFilter.FromValue"/>.</param>
/// <remarks>Prefer building arrays of this struct over ad-hoc string concatenation in application code.</remarks>
public readonly record struct TableFilterCondition(string PropertyName, string Comparison, object? Value);
