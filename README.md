# WebGate.Azure.TableUtils

Extensions for Azure.Data.Tables with typed CRUD clients. Supports complex nested entities, arrays, and IEnumerable via flattened table properties.

The main focus is usage in Azure Functions. Table access uses a Storage Account **connection string**. SAS and other authentication methods are not supported yet, but can be added when required.

**Package:** `WebGate.Azure.TableUtils`  
**License:** [Apache-2.0](LICENSE)

```bash
dotnet add package WebGate.Azure.TableUtils
```

## Target Framework & Versioning

| | |
|---|---|
| Target Framework | `net10.0` |
| Package version | `10.x.x` |

The **NuGet package major version matches the .NET target framework major version**.

- `net10.0` → package version `10.x.x`
- A future uplift to `net11.0` would start at package version `11.0.0`

Within a major line, use minor/patch for library changes that stay on the same TFM.

---

## Entity mapping

POCOs are mapped to Azure Table properties by reflection:

- Readable/writable properties are included.
- Nested objects are **flattened** with `_` as separator (`Parent.Child` → column `Parent_Child`).
- `null` property values are skipped on serialize.
- Value types, `string`, and `byte[]` are stored directly.
- Dedicated converters handle **enums**, **TimeSpan**, **decimal** (InvariantCulture string), **arrays**, and **IEnumerable** (JSON via Newtonsoft.Json).

`ObjectSerializer` (POCO → properties) and `ObjectBuilder` (TableEntity → POCO) implement this mapping. Clients use them automatically.

---

## ExtendedAzureTableClientService

Registers and resolves `TypedAzureTableClient<T>` and `MultiEntityAzureTableClient` instances.

### Create a service

```csharp
var connectionString = "MY_STRING";
var extendedTableService = new ExtendedAzureTableClientService(connectionString);
```

Initialize the service in `Startup` / `Program.cs` of an Azure Function (or host).

### Register TypedAzureTableClients

One table per POCO type:

```csharp
var simplePocoAzureTableClient = extendedTableService.CreateAndRegisterTableClient<SimplePoco>("simplePocoTable");
var parentPocoAzureTableClient = extendedTableService.CreateAndRegisterTableClient<ParentPoco>("parentPocoTable");
```

### Register an already initialized TableClient

```csharp
extendedTableService.AddInitializedTableClient<SimplePoco>(existingTableClient);
```

### Resolve a TypedAzureTableClient

```csharp
var simplePocoAzureTableClient = extendedTableService.GetTypedTableClient<SimplePoco>();
```

Throws `ArgumentOutOfRangeException` if the type was not registered.

### Register a MultiEntityAzureTableClient

Store different entity types in one table. Row keys are prefixed with the registered type name (or a custom prefix):

```csharp
var multiEntityTableClient = extendedTableService.CreateAndRegisterMultiEntityTableClient("allpocos");
multiEntityTableClient.RegisterType<SimplePoco>();
multiEntityTableClient.RegisterType<MainWithParent>("mwp");
multiEntityTableClient.RegisterType<PocoWithListChildren>();
```

`SimplePoco` and `PocoWithListChildren` use their type name as prefix; `MainWithParent` uses `mwp`.

### Resolve a MultiEntityAzureTableClient

```csharp
var multiEntityTableClient = extendedTableService.GetMultiEntityAzureTableClientByTableName("allpocos");
```

The table name used at registration is the lookup key.

---

## TableEntityResult\<T\>

Results from both clients are wrapped in `TableEntityResult<T>`. For `TypedAzureTableClient<T>`, `T` is the POCO type. For `MultiEntityAzureTableClient` list queries, `T` is `object`.

```csharp
public class TableEntityResult<T>(ITableEntity tableEntity, T entity)
{
    public string RowKey { get; set; } = tableEntity.RowKey;
    public string PartitionKey { set; get; } = tableEntity.PartitionKey;
    public ETag ETag { get; set; } = tableEntity.ETag;
    public DateTimeOffset? Timestamp { get; set; } = tableEntity.Timestamp;
    public T Entity { get; set; } = entity;
}
```

---

## TypedAzureTableClient\<T\>

Decorator around `Azure.Data.Tables.TableClient` with POCO serialize/deserialize (including nested entities, arrays, and IEnumerable).

### Get from service

```csharp
var typedTableClient = extendedTableService.GetTypedTableClient<MyPoco>();
```

### Initialize inline

```csharp
var connectionString = "MY_STRING";
var tableClient = new TableClient(connectionString, "MyPoco"); // Azure.Data.Tables
await tableClient.CreateIfNotExistsAsync();
var typedTableClient = new TypedAzureTableClient<MyPoco>(tableClient);
```

Underlying SDK client: `typedTableClient.TableClient` (`GetTableClient()` is obsolete).

Examples below use a client bound to `MyPoco`.

### GetAllAsync()

```csharp
List<TableEntityResult<MyPoco>> pocos = await typedTableClient.GetAllAsync();
```

All rows; no partition filter.

### GetAllAsync(string partitionKey)

```csharp
List<TableEntityResult<MyPoco>> pocos = await typedTableClient.GetAllAsync("mypoco");
```

All rows for the given partition key.

### GetByIdAsync(string id)

```csharp
TableEntityResult<MyPoco>? poco = await typedTableClient.GetByIdAsync("1018301");
```

Looks up by row key `id`. Partition key is `typeof(T).ToString()` (typically the full type name, e.g. `MyNamespace.MyPoco`). Returns `null` if not found.

### GetByIdAsync(string rowKey, string partitionKey)

```csharp
TableEntityResult<MyPoco>? poco = await typedTableClient.GetByIdAsync("9201u819", "mypoco");
```

Returns `null` if not found.

### GetAllByQueryAsync(string? query)

```csharp
var query = $"PartitionKey eq '{partitionKey}'";
List<TableEntityResult<MyPoco>> pocos = await typedTableClient.GetAllByQueryAsync(query);
```

OData filter string as supported by `TableClient.QueryAsync`. Pass `null` for an unfiltered query.

### InsertOrMergeAsync(string rowKey, string partitionKey, object obj)

```csharp
MyPoco poco = new MyPoco();
// populate poco
Azure.Response result = await typedTableClient.InsertOrMergeAsync("001", "SimplePoco", poco);
```

Upsert with `TableUpdateMode.Merge`. The parameter is `object` so partial DTOs (not necessarily `T`) can be merged.

### InsertOrReplaceAsync(string rowKey, string partitionKey, object obj)

```csharp
MyPoco poco = new MyPoco();
// populate poco
Azure.Response result = await typedTableClient.InsertOrReplaceAsync("001", "SimplePoco", poco);
```

Upsert with `TableUpdateMode.Replace`.

### DeleteEntityAsync(string rowKey, string partitionKey)

```csharp
Azure.Response result = await typedTableClient.DeleteEntityAsync("001", "SimplePoco");
```

---

## MultiEntityAzureTableClient

Decorator around `TableClient` with the same mapping capabilities, plus **multiple entity types in one table**. Each registered type gets a row-key prefix (`{prefix}_{rowKey}`). Types must be registered before insert/get-by-type. Unregistered row prefixes on read throw `ArgumentOutOfRangeException`.

### Get from service

```csharp
var multiEntityTableClient = extendedTableService.GetMultiEntityAzureTableClientByTableName("allpocos");
```

### Initialize inline

```csharp
var connectionString = "MY_STRING";
var tableClient = new TableClient(connectionString, "allpocos"); // Azure.Data.Tables
await tableClient.CreateIfNotExistsAsync();
var multiEntityTableClient = new MultiEntityAzureTableClient(tableClient);
multiEntityTableClient.RegisterType<SimplePoco>();
multiEntityTableClient.RegisterType<MainWithParent>("mwp");
multiEntityTableClient.RegisterType<PocoWithListChildren>();
```

Underlying SDK client: `multiEntityTableClient.TableClient` (`GetTableClient()` is obsolete).

Examples below assume `SimplePoco`, `MainWithParent`, and `PocoWithListChildren` are registered.

### GetAllAsync()

```csharp
List<TableEntityResult<object>> allPocos = await multiEntityTableClient.GetAllAsync();
List<SimplePoco> simplePocos = allPocos.Select(res => res.Entity).OfType<SimplePoco>().ToList();
```

### GetAllAsync(string partitionKey)

```csharp
List<TableEntityResult<object>> allPocos = await multiEntityTableClient.GetAllAsync("mypoco");
List<SimplePoco> simplePocos = allPocos.Select(res => res.Entity).OfType<SimplePoco>().ToList();
```

### GetByIdAsync\<T\>(string rowKey, string partitionKey)

```csharp
TableEntityResult<MyPoco>? poco = await multiEntityTableClient.GetByIdAsync<MyPoco>("9201u819", "mypoco");
```

Resolves the stored row key as `{registeredPrefix}_{rowKey}`. Returns `null` if not found. Throws if `T` is not registered.

### GetAllByQueryAsync(string? query)

```csharp
var query = $"PartitionKey eq '{partitionKey}'";
List<TableEntityResult<object>> allPocos = await multiEntityTableClient.GetAllByQueryAsync(query);
List<SimplePoco> simplePocos = allPocos.Select(res => res.Entity).OfType<SimplePoco>().ToList();
```

### InsertOrMergeAsync\<T\>(string rowKey, string partitionKey, T obj)

```csharp
MyPoco poco = new MyPoco();
// populate poco
Azure.Response result = await multiEntityTableClient.InsertOrMergeAsync("001", "SimplePoco", poco);
```

Stores row key as `{prefix}_001`. Type of `obj` must be registered.

### InsertOrReplaceAsync\<T\>(string rowKey, string partitionKey, T obj)

```csharp
MyPoco poco = new MyPoco();
// populate poco
Azure.Response result = await multiEntityTableClient.InsertOrReplaceAsync("001", "SimplePoco", poco);
```

### DeleteEntityByTypeAsync\<T\>(string rowKey, string partitionKey)

```csharp
Azure.Response result = await multiEntityTableClient.DeleteEntityByTypeAsync<SimplePoco>("001", "SimplePoco");
```

Builds the row key from the registered prefix for `T`. Prefer this when you know the entity type.

### DeleteEntityAsync(string completeRowKey, string partitionKey)

```csharp
Azure.Response result = await multiEntityTableClient.DeleteEntityAsync("SimplePoco_001", "SimplePoco");
```

Deletes by the **full** row key already stored in the table (including prefix). Useful when iterating `GetAllAsync` results (`result.RowKey`).

---

## License

Apache-2.0

---

## Copyright

2026, WebGate Consulting AG
