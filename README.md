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
- Future uplifts follow **.NET LTS** releases only (e.g. the next LTS after .NET 10); the package major then matches that TFM major.

Within a major line, use minor/patch for library changes that stay on the same TFM.

**Project layout (since 10.1.3):** see [docs/architecture-table-utils.md](docs/architecture-table-utils.md).

Public API XML documentation is generated into the NuGet (`GenerateDocumentationFile`); IntelliSense matches the types below.

---

## Contents

- [Target Framework & Versioning](#target-framework--versioning)
- [Modules](#modules)
- [Quick start](#quick-start)
- [Entity mapping](#entity-mapping)
- [ExtendedAzureTableClientService](#extendedazuretableclientservice)
- [TableEntityResult\<T\>](#tableentityresultt)
- [TypedAzureTableClient\<T\>](#typedazuretableclientt)
- [MultiEntityAzureTableClient](#multientityazuretableclient)
- [Build & test](#build--test)
- [Release notes (10.1.x)](#release-notes-101x)

---

## Modules

| Folder (repo) | Namespace | Responsibility |
|---------------|-----------|----------------|
| `Clients/` | `WebGate.Azure.TableUtils` | `TypedAzureTableClient<T>`, `MultiEntityAzureTableClient` |
| `Registry/` | `WebGate.Azure.TableUtils` | `ExtendedAzureTableClientService` |
| `Query/` | `WebGate.Azure.TableUtils` | `TableFilter`, `TableQueryBatch`, `TableQueryTopSelect`, `TableRowKeys`, … |
| `Mapping/` | `WebGate.Azure.TableUtils` | `ObjectSerializer`, `ObjectBuilder` |
| `Converters/` | `WebGate.Azure.TableUtils.Converter` | Enum, decimal, JSON collection converters |
| `Models/` | `WebGate.Azure.TableUtils.Models` | `TableEntityResult<T>` |

---

## Quick start

```csharp
var registry = new ExtendedAzureTableClientService(connectionString);
var client = registry.CreateAndRegisterTableClient<MyRow>("myTable");

await client.InsertOrReplaceAsync(rowKey, partitionKey, myRow);

var filter = TableFilter.PartitionKeyEquals(partitionKey);
List<MyRow> rows = await client.QueryEntitiesAsync(filter);
```

For custom OData, always build filters with `TableFilter` (typed literals, escaping). Use `TableClient` on the wrapper for deletes (`DeleteEntityAsync(partitionKey, rowKey)`).

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

Results from both clients are wrapped in `TableEntityResult<T>`.
For `TypedAzureTableClient<T>`, `T` is the POCO type.
For `MultiEntityAzureTableClient`, list queries use `T = object` (concrete type at runtime via `Entity`);
`GetByIdAsync<T>` returns `TableEntityResult<T>`.

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

Preferred 10.x surface: **upserts + gets** (serialize/deserialize). Use `TableClient` for deletes and other raw SDK calls.

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

### QueryAsync / TableFilter (10.1+)

Build OData filters with `TableFilter` (typed literals, AND/OR):

```csharp
var filter = TableFilter.And(
    TableFilter.PartitionKeyEquals("mypoco"),
    TableFilter.Equal("Status", "Active"));
List<TableEntityResult<MyPoco>> rows = await typedTableClient.QueryAsync(filter);
List<MyPoco> entities = await typedTableClient.QueryEntitiesAsync(filter);
```

Optional early stop / page size:

```csharp
var options = new TableQueryOptions { MaxEntities = 50, MaxPerPage = 50 };
List<MyPoco> newestByRowKey = await typedTableClient.QueryPartitionTakeAsync("DevMailCapture", take: 50);
```

When row keys are not time-ordered, top-N by a numeric column (partition scan):

```csharp
List<MyPoco> top = await typedTableClient.QueryTopByInPartitionAsync(
    "DevMailCapture",
    row => row.CreatedTick ?? 0,
    take: 20,
    descending: true);
```

Structured conditions:

```csharp
var conditions = new[] { new TableFilterCondition("Status", QueryComparisons.EQUAL, "Open") };
string combined = TableFilter.CombineConditions(conditions, TableLogicalOperators.AND);
```

Large `IN`-style lookups (batched OR, avoids URL length limits):

```csharp
List<MyPoco> matches = await TableQueryBatch.QueryEqualsAnyAsync(
    typedTableClient,
    propertyName: "FileLinkId",
    values: candidateIds,
    batchSize: 15);
```

Time-ordered row keys (newest first when scanning by row key):

```csharp
string rowKey = TableRowKeys.BuildTimeOrderedDescending(DateTime.UtcNow, Guid.NewGuid().ToString("N"));
```

Stream without loading the full result set:

```csharp
await foreach (var row in typedTableClient.EnumerateAsync(filter, options))
{
    // ...
}
```

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

### GetAllByQueryAsync(string? query) — obsolete

Prefer `QueryAsync` / `QueryEntitiesAsync` with `TableFilter`. See **QueryAsync / TableFilter** above.

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

### DeleteEntityAsync — obsolete (compile error)

Parameter order is **reversed** vs the Azure SDK:

| | 1st arg | 2nd arg |
|---|---|---|
| This library (obsolete) | `rowKey` | `partitionKey` |
| `TableClient.DeleteEntityAsync` | `partitionKey` | `rowKey` |

```csharp
// old:
await typedTableClient.DeleteEntityAsync("001", "SimplePoco");
// new:
await typedTableClient.TableClient.DeleteEntityAsync("SimplePoco", "001");
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

Preferred 10.x surface: **registry + upserts + gets** (+ `DeleteEntityByTypeAsync` for prefix-aware delete). Raw delete by full row key: `TableClient.DeleteEntityAsync`.

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
var filter = TableFilter.PartitionKeyEquals(partitionKey);
List<TableEntityResult<object>> allPocos = await multiEntityTableClient.GetAllByQueryAsync(filter);
List<SimplePoco> simplePocos = allPocos.Select(res => res.Entity).OfType<SimplePoco>().ToList();
```

OData filter as supported by `TableClient.QueryAsync`. Pass `null` for an unfiltered query. Needed here so row keys are still resolved via the type registry.

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

### DeleteEntityAsync(completeRowKey, partitionKey) — obsolete (compile error)

Parameter order is **reversed** vs the Azure SDK:

| | 1st arg | 2nd arg |
|---|---|---|
| This library (obsolete) | `completeRowKey` | `partitionKey` |
| `TableClient.DeleteEntityAsync` | `partitionKey` | `rowKey` |

```csharp
// old:
await multiEntityTableClient.DeleteEntityAsync(result.RowKey, result.PartitionKey);
// new:
await multiEntityTableClient.TableClient.DeleteEntityAsync(result.PartitionKey, result.RowKey);
```

---

## Build & test

From the repository root:

```bash
dotnet build AzureTableUtils.sln -c Release
dotnet test AzureTableUtils.Tests/AzureTableUtils.Tests.csproj -c Release
```

Integration tests (`AzureTableUtils.IntegrationTests`) require Azurite or a development storage connection string and optional `test.runsettings` (see project README in repo if present).

---

## Release notes (10.1.x)

| Version | Highlights |
|---------|------------|
| **10.1.3** | Thematic folders (`Clients/`, `Query/`, `Mapping/`, …); test layout; no public namespace breaking changes. |
| **10.1.2** | `TableFilterCondition`, `TableQueryBatch`, `TableRowKeys`, `EnumerateAsync` / `QueryEntitiesAsync`, partition take & top-by helpers; `TypedAzureTableClient<T> where T : class`. |
| **10.1.1** | `TableFilter`, `TableQueryOptions`, `TableQueryTopSelect`, extended `QueryAsync` overload. |
| **10.1.0** | .NET 10 uplift; package major aligned with TFM. |

Patch releases keep existing `WebGate.Azure.TableUtils` namespaces for NuGet consumers.

---

## License

Apache-2.0

---

## Copyright

2026, WebGate Consulting AG
