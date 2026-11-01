using WebGate.Azure.TableUtils;

namespace WebGate.Azure.TableUtils.Tests;

[TestClass]
public class TableRowKeysTests
{
    [TestMethod]
    public void BuildTimeOrderedDescending_UsesInvertedTickPrefix()
    {
        var utc = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var rowKey = TableRowKeys.BuildTimeOrderedDescending(utc, "ABC");

        var expectedInverted = long.MaxValue - utc.Ticks;
        StringAssert.StartsWith(rowKey, $"{expectedInverted:D19}_ABC");
    }
}
