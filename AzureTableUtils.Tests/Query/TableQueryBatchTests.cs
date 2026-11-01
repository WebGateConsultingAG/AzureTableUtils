using WebGate.Azure.TableUtils;

namespace WebGate.Azure.TableUtils.Tests;

[TestClass]
public class TableQueryBatchTests
{
    [TestMethod]
    public void DistinctNonEmpty_DropsBlankAndDuplicates()
    {
        CollectionAssert.AreEqual(
            new List<string> { "a", "b" },
            TableQueryBatch.DistinctNonEmpty(["a", " ", "a", "b", null]));
    }

    [TestMethod]
    public void DistinctNonEmpty_WhenNull_ReturnsEmpty()
    {
        Assert.AreEqual(0, TableQueryBatch.DistinctNonEmpty(null!).Count);
    }

    [TestMethod]
    public async Task QueryEqualsAnyAsync_ReturnsEmptyWhenNoValues()
    {
        var client = new TypedAzureTableClient<object>(new TableClient("UseDevelopmentStorage=true", "noop"));

        var result = await TableQueryBatch.QueryEqualsAnyAsync(
            client,
            "Id",
            [],
            batchSize: 2);

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void QueryEqualsAnyAsync_ThrowsWhenBatchSizeInvalid()
    {
        var client = new TypedAzureTableClient<object>(new TableClient("UseDevelopmentStorage=true", "noop"));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            TableQueryBatch.QueryEqualsAnyAsync(client, "Id", ["x"], batchSize: 0).GetAwaiter().GetResult());
    }
}
