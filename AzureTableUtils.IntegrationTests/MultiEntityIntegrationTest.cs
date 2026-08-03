using WebGate.Azure.TableUtils;
using WebGate.Azure.TableUtils.Models;
using WebGate.Azure.TableUtils.Test;

namespace AzureTableUtils.IntegrationTests;

[TestClass]
public class MultiEntityIntegrationTest
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
        var multiEntityTableClient = _extendedTableService.CreateAndRegisterMultiEntityTableClient("metests");
        multiEntityTableClient.RegisterType<SimplePoco>();
        multiEntityTableClient.RegisterType<MainWithParent>("mwp");
        multiEntityTableClient.RegisterType<PocoWihtListChildren>();
    }

    [TestMethod]
    public async Task TestMEClientCreateReadDeleted()
    {
        Assert.IsNotNull(_extendedTableService);
        var meTableClient = _extendedTableService.GetMultiEntityAzureTableClientByTableName("metests");
        Assert.IsNotNull(meTableClient);
        string partitionId = await GenerateDataset(meTableClient);
        var allPocoResult = await meTableClient.GetAllAsync(partitionId);
        Assert.IsNotNull(allPocoResult);
        Assert.AreEqual(13, allPocoResult.Count);
        foreach (var resultPoco in allPocoResult)
        {
            await meTableClient.TableClient.DeleteEntityAsync(resultPoco.PartitionKey, resultPoco.RowKey);
        }
        var allPocoResult2 = await meTableClient.GetAllAsync(partitionId);
        Assert.IsNotNull(allPocoResult2);
        Assert.AreEqual(0, allPocoResult2.Count);
    }

    [TestMethod]
    public async Task TestMEClientTypeExtraction()
    {
        Assert.IsNotNull(_extendedTableService);
        var meTableClient = _extendedTableService.GetMultiEntityAzureTableClientByTableName("metests");
        Assert.IsNotNull(meTableClient);
        string partitionId = await GenerateDataset(meTableClient);
        var allPocoResult = await meTableClient.GetAllAsync(partitionId);
        Assert.IsNotNull(allPocoResult);
        Assert.AreEqual(13, allPocoResult.Count);
        List<SimplePoco> simplePocos = allPocoResult.Select(res => res.Entity).OfType<SimplePoco>().ToList();
        Assert.AreEqual(1, simplePocos.Count);
        List<MainWithParent> mainWithParents = allPocoResult.Select(res => res.Entity).OfType<MainWithParent>().ToList();
        List<PocoWihtListChildren> pocoWihtListChildrens = allPocoResult.Select(res => res.Entity).OfType<PocoWihtListChildren>().ToList();
        Assert.AreEqual(5, pocoWihtListChildrens.Count);
        Assert.AreEqual(7, mainWithParents.Count);
        foreach (var resultPoco in allPocoResult)
        {
            await meTableClient.TableClient.DeleteEntityAsync(resultPoco.PartitionKey, resultPoco.RowKey);
        }
        var allPocoResult2 = await meTableClient.GetAllAsync(partitionId);
        Assert.IsNotNull(allPocoResult2);
        Assert.AreEqual(0, allPocoResult2.Count);
    }

    [TestMethod]
    public async Task TestMEClientTypeInfomationAndRowKey()
    {
        Assert.IsNotNull(_extendedTableService);
        var meTableClient = _extendedTableService.GetMultiEntityAzureTableClientByTableName("metests");
        Assert.IsNotNull(meTableClient);
        string partitionId = await GenerateDataset(meTableClient);
        var allPocoResult = await meTableClient.GetAllAsync(partitionId);
        Assert.IsNotNull(allPocoResult);
        Assert.AreEqual(13, allPocoResult.Count);
        List<TableEntityResult<object>> simplePocos = allPocoResult.Where(res => res.Entity.GetType() == typeof(SimplePoco)).ToList();
        Assert.AreEqual(1, simplePocos.Count(x => x.RowKey.StartsWith(typeof(SimplePoco).Name + "_")));
        List<TableEntityResult<object>> mainWithParents = allPocoResult.Where(res => res.Entity.GetType() == typeof(MainWithParent)).ToList();
        List<TableEntityResult<object>> pocoWihtListChildrens = allPocoResult.Where(res => res.Entity.GetType() == typeof(PocoWihtListChildren)).ToList();
        Assert.AreEqual(5, pocoWihtListChildrens.Count(x => x.RowKey.StartsWith(typeof(PocoWihtListChildren).Name + "_")));
        Assert.AreEqual(7, mainWithParents.Count(x => x.RowKey.StartsWith("mwp_")));
        foreach (var resultPoco in allPocoResult)
        {
            await meTableClient.TableClient.DeleteEntityAsync(resultPoco.PartitionKey, resultPoco.RowKey);
        }
        var allPocoResult2 = await meTableClient.GetAllAsync(partitionId);
        Assert.IsNotNull(allPocoResult2);
        Assert.AreEqual(0, allPocoResult2.Count);
    }

    [TestMethod]
    public async Task TestMECRUDOperationOnSingleEntity()
    {
        Assert.IsNotNull(_extendedTableService);
        var meTableClient = _extendedTableService.GetMultiEntityAzureTableClientByTableName("metests");
        Assert.IsNotNull(meTableClient);
        string partitionId = Guid.NewGuid().ToString();
        string idForEntity = Guid.NewGuid().ToString();
        var simplePoco = SimplePoco.CreateInitializedPoco();
        var resultCreate = await meTableClient.InsertOrMergeAsync(idForEntity, partitionId, simplePoco);
        Assert.AreEqual(204, resultCreate.Status);
        var simplePocoGet = await meTableClient.GetByIdAsync<SimplePoco>(idForEntity, partitionId);
        Assert.IsNotNull(simplePocoGet);
        Assert.AreEqual(simplePoco, simplePocoGet.Entity);
        var simplePocDoUpdate = simplePocoGet.Entity;
        simplePocDoUpdate.EnumValue = SP.VALID;
        var resultUpdate = await meTableClient.InsertOrMergeAsync(idForEntity, partitionId, simplePocDoUpdate);
        Assert.AreEqual(204, resultUpdate.Status);
        var simplePocoUpdated = await meTableClient.GetByIdAsync<SimplePoco>(idForEntity, partitionId);
        Assert.IsNotNull(simplePocoUpdated);
        Assert.AreEqual(SP.VALID, simplePocoUpdated.Entity.EnumValue);
        await meTableClient.DeleteEntityByTypeAsync<SimplePoco>(idForEntity, partitionId);
        var allPocoResult2 = await meTableClient.GetAllAsync(partitionId);
        Assert.IsNotNull(allPocoResult2);
        Assert.AreEqual(0, allPocoResult2.Count);
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        Assert.IsNotNull(_extendedTableService);
        var meTableClient = _extendedTableService.GetMultiEntityAzureTableClientByTableName("metests");
        var allPocoResult = await meTableClient.GetAllAsync();
        foreach (var result in allPocoResult)
        {
            await meTableClient.TableClient.DeleteEntityAsync(result.PartitionKey, result.RowKey);
        }
    }

    private static async Task<string> GenerateDataset(MultiEntityAzureTableClient meTableClient)
    {
        var partitionId = Guid.NewGuid().ToString();
        var simplePoco = SimplePoco.CreateInitializedPoco();
        var result = await meTableClient.InsertOrReplaceAsync(Guid.NewGuid().ToString(), partitionId, simplePoco);
        Assert.AreEqual(204, result.Status);
        for (int i = 0; i < 5; i++)
        {
            var pocoWihtListChildren = PocoWihtListChildren.CreateInitializdedPWLC();
            var resultMwp = await meTableClient.InsertOrReplaceAsync(Guid.NewGuid().ToString(), partitionId, pocoWihtListChildren);
        }
        for (int i = 0; i < 7; i++)
        {
            var mainWithParent = MainWithParent.CreateMainWithParent();
            var resultMwp = await meTableClient.InsertOrReplaceAsync(Guid.NewGuid().ToString(), partitionId, mainWithParent);
        }

        return partitionId;
    }
}