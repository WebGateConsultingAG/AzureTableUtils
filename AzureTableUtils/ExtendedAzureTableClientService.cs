namespace WebGate.Azure.TableUtils;

public class ExtendedAzureTableClientService(string connectionString)
{
    private readonly string _connectionString = connectionString;
    private Dictionary<Type, object> _tableClients = new();
    private Dictionary<string, MultiEntityAzureTableClient> _meTableClients = new();

    public TypedAzureTableClient<T> CreateAndRegisterTableClient<T>(string tableName)
    {
        var tableClient = new TableClient(_connectionString, tableName);
        var typedClient = new TypedAzureTableClient<T>(tableClient);
        _tableClients.Add(typeof(T), typedClient);
        typedClient.TableClient.CreateIfNotExists();
        return typedClient;
    }

    public void AddInitializedTableClient<T>(TableClient tableClient)
    {
        var typedClient = new TypedAzureTableClient<T>(tableClient);
        _tableClients.Add(typeof(T), typedClient);
        typedClient.TableClient.CreateIfNotExists();
    }

    public TypedAzureTableClient<T> GetTypedTableClient<T>()
    {
        if (_tableClients.TryGetValue(typeof(T), out var table))
        {
            return (TypedAzureTableClient<T>)table;
        }
        throw new ArgumentOutOfRangeException(typeof(T).Name + " not found as registered TypedTableClient");
    }

    public MultiEntityAzureTableClient CreateAndRegisterMultiEntityTableClient(string tableName)
    {
        var tableClient = new TableClient(_connectionString, tableName);
        var meClient = new MultiEntityAzureTableClient(tableClient);
        _meTableClients.Add(tableName, meClient);
        meClient.TableClient.CreateIfNotExists();
        return meClient;
    }

    public MultiEntityAzureTableClient GetMultiEntityAzureTableClientByTableName(string tableName)
    {
        return _meTableClients[tableName];
    }
}