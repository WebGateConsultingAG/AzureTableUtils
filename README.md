# WebGate.Azure.TableUtils

Extensions for Azure.Data.Tables with typed CRUD clients. Supports complex nested entities, arrays, and IEnumerable via flattened table properties.

The main focus for this implementation is the usage in Azure Functions. Therefore access to the tables is done with the ConnectionString. SAS and other authentication methods are not supported, but can be implemented when required.

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

## ExtendedAzureTableClientService

The ExtendedAzureTableClientService provides a class to register and access TypedAzureTableClients as well as MultiEntityAzureTableClients.

### Create a new ExtendedAzureTableClientService
With a valid connectionString to an Azure Storage Account V2, the creation is straightforward.

```c#
var connectionString = "MY_STRING";
var extendedTableService = new ExtendedAzureTableClientService(connectionString);
```

We recommend to initialize your ExtendedAzureTableClientService in the Startup/Program.cs in an Azure Function. The registration of TypedAzureTableClients is straightforward.

### Create and register some TypedAzureTableClients
You may have 2 Poco you want to save in 2 different tables. A SimplePoco and a ParentPoco. To register these with a table for each, do the following:

```c#
var simplePocoAzureTableClient = extendedTableService.CreateAndRegisterTableClient<SimplePoco>("simplePojoTable");
var parentPocoAzureTableClient = extendedTableService.CreateAndRegisterTableClient<ParentPoco>("parentPojoTable");
```
### Accessing a TypedAzureTableClient
To get a specific TypeAzureTableClient, use the following approach with the ExtendedAzureTableClientService:
```c#
var simplePocoAzureTableClient = extendedTableService.GetTypedTableClient<SimplePoco>();
```
### Create and register a MultiEntityAzureTableClient
The purpose for a MultiEntityAzureTableClient is to store entities of different types / kinds into the same table. The registration of these types is very easy.
```c#
var multiEntityTableClient = _extendedTableService.CreateAndRegisterMultiEntityTableClient("allpocos");
multiEntityTableClient.RegisterType<SimplePoco>();
multiEntityTableClient.RegisterType<MainWithParent>("mwp");
multiEntityTableClient.RegisterType<PocoWithListChildren>();
```
The example registers a MultiEntityAzureTableClient bound to the table called "allpocos". The types are registered using its TypeName as prefix for the rowkey. MainWithParent is in this example registered with "mwp" as prefix.

### Access a MultiEntityAzureTableClient
Use the following code to access the registered MultiEntityAzureTableClient:
```c#
var multiEntityTableClient = _extendedTableService.GetMultiEntityAzureTableClientByTableName("allpocos");
```
The name of the table during registration is your key. For details about the usage of the client, see below.

## TableEntityResult<T>
Results from TypedAzureTableClient and MultiEntityAzureTableClient are wrapped into the class TableEntityResult. For a result from a TypedAzureTableClient define the type of the client using the generic T. The resulting type from a MultiEntityAzureTableClient is always object. The class has the following signature:
```c#
public class TableEntityResult<T>(ITableEntity tableEntity, T entity)
{
    public string RowKey { get; set; } = tableEntity.RowKey;
    public string PartitionKey { set; get; } = tableEntity.PartitionKey;
    public ETag ETag { get; set; } = tableEntity.ETag;
    public DateTimeOffset? Timestamp { get; set; } = tableEntity.Timestamp;
    public T Entity { get; set; } = entity;
 }
```

## TypedAzureTableClient
The TypedAzureTableClient is a decorator to the AzureTableClient. The main purpose is to extend conversion from and to the defined entity with the capability for complex entities, arrays and IEnumerable's. The client can be initialized via ExtendAzureTableClientService or direct in the code, using the following pattern.

Get the client from the service:
```c#
var typedTableClient = _extendedTableService.GetTypedTableClient<MyPoco>();
```

Initialize inline:
```c#
var connectionString = "MY_STRING"; //String to Azure Storage Account V2
var tableClient = new TableClient(connectionString, "MyPoco"); // From Azure.Data.Table
await tableClient.CreateIfNotExistsAsync();
var typedTableClient = new TypedAzureTableClient<MyPoco>(tableClient);
```

For all examples, we are using a TypedAzureTableClient bound to MyPoco as generic type.
The following operations are provided:

### GetAllAsync()

```c#
List<TableEntityResult<MyPoco>> pocos = await typedTableClient.GetAllAsync();
```

Gets all data from a table and convert it into the specified object type. No partition key is applied.

### GetAllAsync(string partition)

```c#
List<TableEntityResult<MyPoco>> pocos = await typedTableClient.GetAllAsync('mypoco');
```

Gets all data from a table and convert it into the specified object type. A partition key is applied. The current example applies 'mypoco' as partition key.

### GetByIdAsync(string id)

```c#
TableEntityResult<MyPoco>? poco = await typedTableClient.GetByIdAsync('1018301');
```

Gets as specific entity from the table and convert it to the specified object. The name of the type (MyPoco in this example) is used as partition key.
If the id and partition key combination finds no object, null is returned.

### GetByIdAsync(string id, string partition)

```c#
TableEntityResult<MyPoco>? poco = await typedTableClient.GetByIdAsync('9201u819','mypoco');
```

Gets as specific entity form the table and convert it to the specified object. The partition key is the 2nd argument.
If the id and partition key combination finds no object, null is returned.

### GetAllByQueryAsync(TableQuery query)

```c#
var query = $"PartitionKey eq '{partitionKey}'";
List<TableEntityResult<MyPoco>> pocos = await typedTableClient.GetAllByQueryAsync(query);
```

Gets all entities that matches the query.

### InsertOrMergeAsync(string id, string partition, object obj)

```c#
MyPoco poco = new MyPoco();
// Do magicStuff with poco
Azure.Response result = await typedTableClient.InsertOrMergeAsync("001", "SimplePoco", poco);
```

Creates or merges a specific object into the table. The selection is done by id and partition key.

### InsertOrReplaceAsync(string id, string partition, object obj)

```c#
MyPoco poco = new MyPoco();
// Do magicStuff with poco
Azure.Response result = await typedTableClient.InsertOrReplaceAsync("001", "SimplePoco", poco);
```

Creates or replace a specific object into the table. The selection is done by id and partition key.

### DeleteEntryAsync(string id, string partition)

```c#
Azure.Response result = await typedTableClient.DeleteEntityAsync("001", "SimplePoco");
```

Deletes a specific object from the table. The selection is done by id and partition key.

## MultiEntityAzureTableClient
The MultiEntityAzureTableClient is a decorator to the AzureTableClient. The main purpose is to extend the conversion from and to the defined entities with the capability for complex entities, arrays and IEnumerable's. 
<b>The client adds the functionality to support different entity types in one table.</b>
The client can be initialized using ExtendAzureTableClientService or direct in the code, using the following pattern.

Get the client from the service:
```c#
var multiEntityTableClient = _extendedTableService.GetMultiEntityAzureTableClientByTableName("allpocos");
```

Initialize inline:
```c#
var connectionString = "MY_STRING"; //String to Azure Storage Account V2
var tableClient = new TableClient(connectionString, "allpocos"); // From Azure.Data.Table
await tableClient.CreateIfNotExistsAsync();
var multiEntityTableClient = new MultiEntityAzureTableClient(tableClient);
multiEntityTableClient.RegisterType<SimplePoco>();
multiEntityTableClient.RegisterType<MainWithParent>("mwp");
multiEntityTableClient.RegisterType<PocoWithListChildren>();
```

For all examples, we are using a MultiEntityAzureTableClient with SimplePoco, MainWithParent und PocoWithListChildren as registered entity types.
The following operations are provided:

### GetAllAsync()

```c#
List<TableEntityResult<object>> allPocos = await multiEntityTableClient.GetAllAsync();
```
Gets all data from a table and convert them it the specified object. No partition key is applied. To extract a specific entity type, use the following pattern:

```c#
List<SimplePoco> simplePocos = allPocos.Select(res => res.Entity).OfType<SimplePoco>().ToList();
```

### GetAllAsync(string partition)

```c#
List<TableEntityResult<object>> pocos = await multiEntityTableClient.GetAllAsync('mypoco');
```

Gets all data from a table and convert it into the specified object. A partition key is applied. The current example applies 'mypoco' as partition key. To extract a specific entity type, use the following pattern:

```c#
List<SimplePoco> simplePocos = allPocos.Select(res => res.Entity).OfType<SimplePoco>().ToList();
```

### GetByIdAsync\<T>\(string id, string partition)

```c#
TableEntityResult<MyPoco>? poco = await multiEntityTableClient.GetByIdAsync<MyPoco>('9201u819','mypoco');
```

Gets as specific entity from the table and convert it to the specified object. The partition key is the 2nd argument.
If the id and partition key combination finds no object, null is returned.

### GetAllByQueryAsync(string query)

```c#
var query = $"PartitionKey eq '{partitionKey}'";
List<TableEntityResult<MyPoco>> pocos = await multiEntityTableClient.GetAllByQueryAsync(query);
```

Gets alls entities that matches the query. To extract a specific entity type, use the following pattern:

```c#
List<SimplePoco> simplePocos = allPocos.Select(res => res.Entity).OfType<SimplePoco>().ToList();
```

### InsertOrMergeAsync(string id, string partition, object obj)

```c#
MyPoco poco = new MyPoco();
// Do magicStuff with poco
Azure.Response result = await multiEntityTableClient.InsertOrMergeAsync("001", "SimplePoco", poco);
```

Creates or merges a specific object into the table. The selection is done by id and partition key.

### InsertOrReplaceAsync(string id, string partition, object obj)

```c#
MyPoco poco = new MyPoco();
// Do magicStuff with poco
Azure.Response result = await multiEntityTableClient.InsertOrReplaceAsync("001", "SimplePoco", poco);
```

Creates or replaces a specific object into the table. The selection is done by id and partition key.

### DeleteEntryAsync\<T>\(string id, string partition)

```c#
Azure.Response result = await multiEntityTableClient.DeleteEntityAsync<SimplePoco>("001", "SimplePoco");
```

Deletes a specific object from the table. The selection is done by id and partition key. The entity type must be specified, otherwise the client is not capable to calculate the correct row key.

---

## Code Quality Check SonarCloud.io

[![Quality gate](https://sonarcloud.io/api/project_badges/quality_gate?project=CloudTableUtils&token=b8ea0b7d7b29c7e13fb260bae8cf0d3eb36597ec)](https://sonarcloud.io/dashboard?id=CloudTableUtils)

---

## Licence

Apache V 2.0

---

## Copyright

2024, WebGate Consulting AG
