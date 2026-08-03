using WebGate.Azure.TableUtils;
using WebGate.Azure.TableUtils.Test;

namespace AzureTableUtils.IntegrationTests;

[TestClass]
public class LargeDatasetIntegrationTest
{
    public TestContext? TestContext { get; set; }
    public string? ConnectionString { get; set; }
    public ExtendedAzureTableClientService? _extendedTableService;

    [TestInitialize]
    public void TestInitialize()
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        ConnectionString = (string)TestContext.Properties["CONNECTION_STRING"];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        Assert.IsNotNull(ConnectionString);
        _extendedTableService = new ExtendedAzureTableClientService(ConnectionString);
        _extendedTableService.CreateAndRegisterTableClient<SimplePoco>("largescaletest");
    }

    [TestMethod]
    public async Task TestSimplePocoCreateRead1500EntriesDeleted()
    {
        Assert.IsNotNull(_extendedTableService);
        var typedTableClient = _extendedTableService.GetTypedTableClient<SimplePoco>();
        Assert.IsNotNull(typedTableClient);
        var partitionId = Guid.NewGuid().ToString();
        for (int i = 0; i < 1500; i++)
        {
            var id = Guid.NewGuid().ToString();
            var poco = SimplePoco.CreateInitializedPoco();
            poco.IntValue = 1;
            var response = await typedTableClient.InsertOrReplaceAsync(id, partitionId, poco);
            Assert.AreEqual(204, response.Status);
        }
        var allPocoResult = await typedTableClient.GetAllAsync(partitionId);
        Assert.IsNotNull(allPocoResult);
        Assert.AreEqual(1500, allPocoResult.Count);
        foreach (var result in allPocoResult)
        {
            await typedTableClient.DeleteEntityAsync(result.RowKey, result.PartitionKey);
        }
        var allPocoResult2 = await typedTableClient.GetAllAsync(partitionId);
        Assert.IsNotNull(allPocoResult2);
        Assert.AreEqual(0, allPocoResult2.Count);
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        Assert.IsNotNull(_extendedTableService);
        var typedTableClient = _extendedTableService.GetTypedTableClient<SimplePoco>();
        var allPocoResult = await typedTableClient.GetAllAsync();
        foreach (var result in allPocoResult)
        {
            await typedTableClient.DeleteEntityAsync(result.RowKey, result.PartitionKey);
        }
    }
}