using WebGate.Azure.TableUtils;

namespace WebGate.Azure.TableUtils.Tests;

[TestClass]
public class TableFilterCombineConditionsTests
{
    [TestMethod]
    public void CombineConditions_OrJoinsConditions()
    {
        TableFilterCondition[] conditions =
        [
            new("A", QueryComparisons.EQUAL, "1"),
            new("B", QueryComparisons.EQUAL, "2")
        ];

        var result = TableFilter.CombineConditions(conditions, TableLogicalOperators.OR);

        Assert.AreEqual(
            TableFilter.Or(TableFilter.Equal("A", "1"), TableFilter.Equal("B", "2")),
            result);
    }

    [TestMethod]
    public void CombineConditions_DefaultsToAnd()
    {
        TableFilterCondition[] conditions = [new("Status", QueryComparisons.EQUAL, "Open")];

        var result = TableFilter.CombineConditions(conditions);

        Assert.AreEqual(TableFilter.Equal("Status", "Open"), result);
    }
}
