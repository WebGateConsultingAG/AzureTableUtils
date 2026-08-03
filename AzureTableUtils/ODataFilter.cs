namespace WebGate.Azure.TableUtils;

internal static class ODataFilter
{
    internal static string EscapeString(string value) => value.Replace("'", "''");

    internal static string PartitionKeyEquals(string partitionKey) =>
        $"PartitionKey eq '{EscapeString(partitionKey)}'";
}
