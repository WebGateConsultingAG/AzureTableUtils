using Azure.Data.Tables;

namespace WebGate.Azure.TableUtils;

/// <summary>
/// Creates, registers, and resolves <see cref="TypedAzureTableClient{T}"/> and
/// <see cref="MultiEntityAzureTableClient"/> instances for one storage account connection string.
/// </summary>
/// <remarks>
/// Typical host setup: one service instance per Function app / API, register tables at startup,
/// resolve clients from DI facades. Each registration calls <c>CreateIfNotExists</c> on the underlying table.
/// </remarks>
public class ExtendedAzureTableClientService(string connectionString)
{
    private readonly string _connectionString = connectionString;
    private readonly Dictionary<Type, object> _tableClients = [];
    private readonly Dictionary<string, MultiEntityAzureTableClient> _meTableClients = [];

    /// <summary>
    /// Creates a table (if missing), wraps it in <see cref="TypedAzureTableClient{T}"/>, and registers by <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">POCO type bound to this table; must be a reference type.</typeparam>
    /// <param name="tableName">Azure table name.</param>
    /// <returns>The registered typed client.</returns>
    /// <exception cref="ArgumentException">Thrown when <typeparamref name="T"/> is already registered.</exception>
    public TypedAzureTableClient<T> CreateAndRegisterTableClient<T>(string tableName) where T : class
    {
        var tableClient = new TableClient(_connectionString, tableName);
        var typedClient = new TypedAzureTableClient<T>(tableClient);
        _tableClients.Add(typeof(T), typedClient);
        typedClient.TableClient.CreateIfNotExists();
        return typedClient;
    }

    /// <summary>
    /// Registers an externally constructed <see cref="TableClient"/> for <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">POCO type bound to the table; must be a reference type.</typeparam>
    /// <param name="tableClient">Initialized SDK client (same connection as this service).</param>
    /// <exception cref="ArgumentException">Thrown when <typeparamref name="T"/> is already registered.</exception>
    public void AddInitializedTableClient<T>(TableClient tableClient) where T : class
    {
        var typedClient = new TypedAzureTableClient<T>(tableClient);
        _tableClients.Add(typeof(T), typedClient);
        typedClient.TableClient.CreateIfNotExists();
    }

    /// <summary>
    /// Returns the registered <see cref="TypedAzureTableClient{T}"/> for <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Previously registered POCO type.</typeparam>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <typeparamref name="T"/> was not registered.</exception>
    public TypedAzureTableClient<T> GetTypedTableClient<T>() where T : class
    {
        if (_tableClients.TryGetValue(typeof(T), out var table))
        {
            return (TypedAzureTableClient<T>)table;
        }

        throw new ArgumentOutOfRangeException(typeof(T).Name + " not found as registered TypedTableClient");
    }

    /// <summary>
    /// Creates a multi-entity table (if missing) and registers it by <paramref name="tableName"/>.
    /// </summary>
    /// <param name="tableName">Azure table name and lookup key for <see cref="GetMultiEntityAzureTableClientByTableName"/>.</param>
    /// <returns>Client with an empty type registry; call <c>RegisterType&lt;T&gt;()</c> before use.</returns>
    public MultiEntityAzureTableClient CreateAndRegisterMultiEntityTableClient(string tableName)
    {
        var tableClient = new TableClient(_connectionString, tableName);
        var meClient = new MultiEntityAzureTableClient(tableClient);
        _meTableClients.Add(tableName, meClient);
        meClient.TableClient.CreateIfNotExists();
        return meClient;
    }

    /// <summary>
    /// Returns a previously registered <see cref="MultiEntityAzureTableClient"/>.
    /// </summary>
    /// <param name="tableName">Name used in <see cref="CreateAndRegisterMultiEntityTableClient"/>.</param>
    /// <exception cref="KeyNotFoundException">Thrown when <paramref name="tableName"/> was not registered.</exception>
    public MultiEntityAzureTableClient GetMultiEntityAzureTableClientByTableName(string tableName)
    {
        return _meTableClients[tableName];
    }
}
