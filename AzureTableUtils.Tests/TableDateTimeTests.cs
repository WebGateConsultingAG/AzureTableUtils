namespace WebGate.Azure.TableUtils.Test;

[TestClass]
public class TableDateTimeTests
{
    [TestMethod]
    public void EnsureUtc_LeavesUtcUnchanged()
    {
        var utc = new DateTime(2026, 8, 21, 12, 0, 0, DateTimeKind.Utc);
        DateTime result = TableDateTime.EnsureUtc(utc);
        Assert.AreEqual(DateTimeKind.Utc, result.Kind);
        Assert.AreEqual(utc, result);
    }

    [TestMethod]
    public void EnsureUtc_ConvertsLocalToUtc()
    {
        var local = new DateTime(2026, 8, 21, 14, 0, 0, DateTimeKind.Local);
        DateTime result = TableDateTime.EnsureUtc(local);
        Assert.AreEqual(DateTimeKind.Utc, result.Kind);
        Assert.AreEqual(local.ToUniversalTime(), result);
    }

    [TestMethod]
    public void EnsureUtc_TreatsUnspecifiedAsUtcTicks()
    {
        var unspecified = new DateTime(2026, 8, 21, 12, 0, 0, DateTimeKind.Unspecified);
        DateTime result = TableDateTime.EnsureUtc(unspecified);
        Assert.AreEqual(DateTimeKind.Utc, result.Kind);
        Assert.AreEqual(unspecified.Ticks, result.Ticks);
    }

    [TestMethod]
    public void EnsureUtc_NullableNullReturnsNull()
    {
        DateTime? result = TableDateTime.EnsureUtc((DateTime?)null);
        Assert.IsNull(result);
    }
}
