namespace WebGate.Azure.TableUtils;

internal static class ODataFilter
{
    internal static string EscapeString(string value)
    {
        return value.Replace("'", "''");
    }

    internal static string PartitionKeyEquals(string partitionKey)
    {
        return $"PartitionKey eq '{EscapeString(partitionKey)}'";
    }
}
