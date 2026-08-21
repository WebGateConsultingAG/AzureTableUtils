namespace WebGate.Azure.TableUtils.Test;

[TestClass]
public class TableFilterTests
{
    [TestMethod]
    public void Escape_DoublesSingleQuotes()
    {
        Assert.AreEqual("O''Brien", TableFilter.Escape("O'Brien"));
    }

    [TestMethod]
    public void Equal_FormatsStringWithQuotes()
    {
        Assert.AreEqual("Status eq 'READY_TO_SEND'", TableFilter.Equal("Status", "READY_TO_SEND"));
    }

    [TestMethod]
    public void Equal_EscapesQuotesInString()
    {
        Assert.AreEqual("Name eq 'O''Brien'", TableFilter.Equal("Name", "O'Brien"));
    }

    [TestMethod]
    public void Equal_FormatsInt()
    {
        Assert.AreEqual("Count eq 42", TableFilter.Equal("Count", 42));
    }

    [TestMethod]
    public void Equal_FormatsLongWithSuffix()
    {
        Assert.AreEqual("Tick eq 100008937819L", TableFilter.Equal("Tick", 100008937819L));
    }

    [TestMethod]
    public void Equal_FormatsBoolLowercase()
    {
        Assert.AreEqual("IsArchived eq true", TableFilter.Equal("IsArchived", true));
        Assert.AreEqual("IsArchived eq false", TableFilter.Equal("IsArchived", false));
    }

    [TestMethod]
    public void Equal_FormatsGuidLiteral()
    {
        var guid = Guid.Parse("deadbeef-0000-0000-0000-000000000001");
        Assert.AreEqual("Id eq guid'deadbeef-0000-0000-0000-000000000001'", TableFilter.Equal("Id", guid));
    }

    [TestMethod]
    public void LessThanOrEqual_FormatsDateTimeOffsetWithSevenFractionalDigits()
    {
        var value = new DateTimeOffset(2026, 8, 21, 12, 30, 45, 123, TimeSpan.Zero);
        var filter = TableFilter.LessThanOrEqual("DeliverDate", value);

        Assert.AreEqual("DeliverDate le datetime'2026-08-21T12:30:45.1230000Z'", filter);
    }

    [TestMethod]
    public void Compare_DateTimeOffset_ConvertsToUtc()
    {
        var value = new DateTimeOffset(2026, 8, 21, 14, 0, 0, TimeSpan.FromHours(2));
        var filter = TableFilter.Compare("DeliverDate", QueryComparisons.LESS_THAN_OR_EQUAL, value);

        Assert.AreEqual("DeliverDate le datetime'2026-08-21T12:00:00.0000000Z'", filter);
    }

    [TestMethod]
    public void PartitionKeyEquals_EscapesValue()
    {
        Assert.AreEqual("PartitionKey eq 'MassMailHeader'", TableFilter.PartitionKeyEquals("MassMailHeader"));
        Assert.AreEqual("PartitionKey eq 'O''Brien'", TableFilter.PartitionKeyEquals("O'Brien"));
    }

    [TestMethod]
    public void RowKeyEquals_UsesRowKeyProperty()
    {
        Assert.AreEqual("RowKey eq '001'", TableFilter.RowKeyEquals("001"));
    }

    [TestMethod]
    public void Combine_TreatsNullAndEmptyAsIdentity()
    {
        Assert.AreEqual("Status eq 'A'", TableFilter.Combine(TableOperators.AND, null, "Status eq 'A'"));
        Assert.AreEqual("Status eq 'A'", TableFilter.Combine(TableOperators.AND, "Status eq 'A'", null));
        Assert.AreEqual("Status eq 'A'", TableFilter.Combine(TableOperators.AND, string.Empty, "Status eq 'A'"));
        Assert.AreEqual(string.Empty, TableFilter.Combine(TableOperators.AND, null, null));
    }

    [TestMethod]
    public void And_WrapsBothSides()
    {
        var filter = TableFilter.And("Status eq 'READY'", "DeliverDate le datetime'2026-01-01T00:00:00.0000000Z'");
        Assert.AreEqual("(Status eq 'READY') and (DeliverDate le datetime'2026-01-01T00:00:00.0000000Z')", filter);
    }

    [TestMethod]
    public void And_ParamsDropsEmptyParts()
    {
        var filter = TableFilter.And("Status eq 'READY'", string.Empty, "Creator eq 'ann'");
        Assert.AreEqual("(Status eq 'READY') and (Creator eq 'ann')", filter);
    }

    [TestMethod]
    public void Or_CombinesFilters()
    {
        var filter = TableFilter.Or("Status eq 'A'", "Status eq 'B'");
        Assert.AreEqual("(Status eq 'A') or (Status eq 'B')", filter);
    }

    [TestMethod]
    public void FromValue_DispatchesByRuntimeType()
    {
        Assert.AreEqual("Name eq 'ann'", TableFilter.FromValue("Name", QueryComparisons.EQUAL, "ann"));
        Assert.AreEqual("Count eq 3", TableFilter.FromValue("Count", QueryComparisons.EQUAL, 3));
        Assert.AreEqual("Flag eq true", TableFilter.FromValue("Flag", QueryComparisons.EQUAL, true));
        Assert.AreEqual("EnumValue eq 'INVALID'", TableFilter.FromValue("EnumValue", QueryComparisons.EQUAL, SP.INVALID));
    }

    [TestMethod]
    public void FromValue_DateTimeUsesEnsureUtc()
    {
        var unspecified = new DateTime(2026, 8, 21, 12, 0, 0, DateTimeKind.Unspecified);
        var filter = TableFilter.FromValue("Timestamp", QueryComparisons.GREATER_THAN, unspecified);
        Assert.AreEqual("Timestamp gt datetime'2026-08-21T12:00:00.0000000Z'", filter);
    }

    [TestMethod]
    public void FromValue_NullIsEmptyStringLiteral()
    {
        Assert.AreEqual("Name eq ''", TableFilter.FromValue("Name", QueryComparisons.EQUAL, null));
    }

    [TestMethod]
    public void NotEqual_FormatsString()
    {
        Assert.AreEqual("Status ne 'CLOSED'", TableFilter.NotEqual("Status", "CLOSED"));
    }

    [TestMethod]
    public void Compare_ThrowsWhenPropertyNameMissing()
    {
        Assert.ThrowsException<ArgumentException>(() => TableFilter.Equal(string.Empty, "x"));
    }
}
