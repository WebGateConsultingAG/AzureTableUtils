namespace WebGate.Azure.TableUtils.Test;

[TestClass]
public class BuilderListArrayTest
{
    [TestMethod]
    public void TestPocoWithEmptyArray()
    {
        SimplePocoWithArray spwa = SimplePocoWithArray.CreateEmptySimplePocoWithArray();
        IDictionary<string, object> allEntities = ObjectSerializer.Serialize(spwa);
        TableEntity tableEntity = new TableEntity(allEntities);
        SimplePocoWithArray build = ObjectBuilder.Build<SimplePocoWithArray>(tableEntity);
        Assert.AreEqual(0, allEntities.Count);
        Assert.IsNull(build.DateTimeArray);
    }

    [TestMethod]
    public void TestPocoWithInitializedArray()
    {
        SimplePocoWithArray spwa = SimplePocoWithArray.CreateFilledSimplePocoWithArray();
        IDictionary<string, object> allEntities = ObjectSerializer.Serialize(spwa);
        TableEntity tableEntity = new TableEntity(allEntities);
        SimplePocoWithArray build = ObjectBuilder.Build<SimplePocoWithArray>(tableEntity);
        Assert.AreEqual(3, allEntities.Count);
        CollectionAssert.AreEqual(build.DateTimeArray, spwa.DateTimeArray);
    }
}