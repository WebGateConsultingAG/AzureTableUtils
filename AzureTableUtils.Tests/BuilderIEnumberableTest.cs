namespace WebGate.Azure.TableUtils.Test;

[TestClass]
public class BuilderIEnumberableTest
{
    [TestMethod]
    public void TestListObjects()
    {
        PocoWihtListChildren pwlc = PocoWihtListChildren.CreateInitializdedPWLC();
        IDictionary<string, object> allEntities = ObjectSerializer.Serialize(pwlc);
        TableEntity tableEntity = new TableEntity(allEntities);
        PocoWihtListChildren build = ObjectBuilder.Build<PocoWihtListChildren>(tableEntity);
        Assert.IsNotNull(build.Children);
        Assert.AreEqual(2, allEntities.Count);

        Assert.AreEqual(3, build.Children.Count);
        CollectionAssert.AreEqual(build.Children, pwlc.Children);
    }
}