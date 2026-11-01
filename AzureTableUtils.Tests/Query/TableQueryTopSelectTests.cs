using WebGate.Azure.TableUtils.Models;

namespace WebGate.Azure.TableUtils.Tests;

[TestClass]
public class TableQueryTopSelectTests
{
    private sealed class SortableRow
    {
        public long Score { get; set; }
    }

    [TestMethod]
    public async Task SelectTopByAsync_WhenTakeZero_ReturnsEmpty()
    {
        var tableClient = new TypedAzureTableClient<SortableRow>(new TableClient("UseDevelopmentStorage=true", "noop"));

        var result = await TableQueryTopSelect.SelectTopByAsync(
            tableClient,
            filter: "PartitionKey eq 'x'",
            orderKeySelector: row => row.Score,
            take: 0);

        Assert.AreEqual(0, result.Count);
    }
}
