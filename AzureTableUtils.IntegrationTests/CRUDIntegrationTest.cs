using WebGate.Azure.TableUtils;
using WebGate.Azure.TableUtils.Test;

namespace AzureTableUtils.IntegrationTests;

[TestClass]
public class CRUDIntegrationTest
{
    public TestContext? TestContext { get; set; }
    public string? ConnectionString { get; set; }

    [TestInitialize]
    public void TestInitialize()
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        ConnectionString = (string)TestContext.Properties["CONNECTION_STRING"];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
    }

    [TestMethod]
    public async Task TestSimplePocoCreateReadDeleted()
    {
        var tableClient = new TableClient(ConnectionString, "SimplePojoTable");
        await tableClient.CreateIfNotExistsAsync();
        var typedTableClient = new TypedAzureTableClient<SimplePoco>(tableClient);
        var poco = SimplePoco.CreateInitializedPoco();
        var id = Guid.NewGuid().ToString();
        var response = await typedTableClient.InsertOrReplaceAsync(id, "poco", poco);
        Assert.AreEqual(204, response.Status);
        var tableEntityRespose = await typedTableClient.GetByIdAsync(id, "poco");
        Assert.IsNotNull(tableEntityRespose);
        Assert.AreEqual(poco, tableEntityRespose.Entity);
        var deleteResponse = await typedTableClient.TableClient.DeleteEntityAsync("poco", id);
        Assert.AreEqual(204, deleteResponse.Status);
    }

    [TestMethod]
    public async Task TestSimplePocoUpdateAsMerge()
    {
        var tableClient = new TableClient(ConnectionString, "SimplePojoTable");
        await tableClient.CreateIfNotExistsAsync();
        var typedTableClient = new TypedAzureTableClient<SimplePoco>(tableClient);
        var poco = SimplePoco.CreateInitializedPoco();
        var id = Guid.NewGuid().ToString();
        var response = await typedTableClient.InsertOrReplaceAsync(id, "poco", poco);
        Assert.AreEqual(204, response.Status);
        var pocoUpdate = new SimplePocoPart();
        pocoUpdate.EnumValue = SP.VALID;
        var responseUpdate = await typedTableClient.InsertOrMergeAsync(id, "poco", pocoUpdate);
        Assert.AreEqual(204, responseUpdate.Status);
        var tableEntityRespose = await typedTableClient.GetByIdAsync(id, "poco");
        Assert.IsNotNull(tableEntityRespose);
        poco.EnumValue = SP.VALID;
        Assert.AreEqual(poco, tableEntityRespose.Entity);
        var deleteResponse = await typedTableClient.TableClient.DeleteEntityAsync("poco", id);
        Assert.AreEqual(204, deleteResponse.Status);
    }

    [TestMethod]
    public async Task TestSimplePocoUpdateAsReplace()
    {
        var tableClient = new TableClient(ConnectionString, "SimplePojoTable");
        await tableClient.CreateIfNotExistsAsync();
        var typedTableClient = new TypedAzureTableClient<SimplePoco>(tableClient);
        var poco = SimplePoco.CreateInitializedPoco();
        var id = Guid.NewGuid().ToString();
        var response = await typedTableClient.InsertOrReplaceAsync(id, "poco", poco);
        Assert.AreEqual(204, response.Status);
        var pocoUpdate = SimplePoco.CreateInitializedPoco();
        pocoUpdate.EnumValue = SP.VALID;
        pocoUpdate.GuidValue = new Guid();
        pocoUpdate.IntValue = 9383;
        var responseUpdate = await typedTableClient.InsertOrReplaceAsync(id, "poco", pocoUpdate);
        Assert.AreEqual(204, responseUpdate.Status);
        var tableEntityRespose = await typedTableClient.GetByIdAsync(id, "poco");
        Assert.IsNotNull(tableEntityRespose);
        Assert.AreEqual(pocoUpdate, tableEntityRespose.Entity);
        var deleteResponse = await typedTableClient.TableClient.DeleteEntityAsync("poco", id);
        Assert.AreEqual(204, deleteResponse.Status);
    }

    [TestMethod]
    public async Task TestCascadedPocoCreateReadDeleted()
    {
        var tableClient = new TableClient(ConnectionString, "Cascaded");
        await tableClient.CreateIfNotExistsAsync();
        var typedTableClient = new TypedAzureTableClient<MainWithParent>(tableClient);
        var poco = MainWithParent.CreateMainWithParent();
        var id = Guid.NewGuid().ToString();
        var response = await typedTableClient.InsertOrReplaceAsync(id, "poco", poco);
        Assert.AreEqual(204, response.Status);
        var tableEntityRespose = await typedTableClient.GetByIdAsync(id, "poco");
        Assert.IsNotNull(tableEntityRespose);
        Assert.AreEqual(poco, tableEntityRespose.Entity);
        var deleteResponse = await typedTableClient.TableClient.DeleteEntityAsync("poco", id);
        Assert.AreEqual(204, deleteResponse.Status);
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        await TableCleanup<MainWithParent>("Cascaded");
        await TableCleanup<SimplePoco>("SimplePojoTable");
    }

    private async Task TableCleanup<T>(string tableName)
    {
        var tableClient = new TableClient(ConnectionString, tableName);
        var typedTableClient = new TypedAzureTableClient<T>(tableClient);
        var allPocoResult = await typedTableClient.GetAllAsync();
        foreach (var result in allPocoResult)
        {
            await tableClient.DeleteEntityAsync(result.PartitionKey, result.RowKey);
        }
    }
}