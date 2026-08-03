namespace WebGate.Azure.TableUtils.Test;

[TestClass]
public class BuilderTests
{
    [TestMethod]
    public void TestBuildAllEnitiesFromSimplePoco()
    {
        SimplePoco spo = SimplePoco.CreateInitializedPoco();
        IDictionary<string, object> allEntities = ObjectSerializer.Serialize(spo);
        Assert.AreEqual(10, allEntities.Count);
        TableEntity tableEntity = new TableEntity(allEntities);
        SimplePoco build = ObjectBuilder.Build<SimplePoco>(tableEntity);
        Assert.IsNotNull(build);
        Assert.AreEqual(spo.Id, build.Id);
        Assert.AreEqual(spo.DoubleValue, build.DoubleValue);
        Assert.AreEqual(spo.DecimalValue, build.DecimalValue);
        Assert.AreEqual(spo.DTOValue, build.DTOValue);
        Assert.AreEqual(spo.DTValue, build.DTValue);
        Assert.AreEqual(spo.GuidValue, build.GuidValue);
        Assert.AreEqual(spo.IntValue, build.IntValue);
        Assert.AreEqual(spo.LongValue, build.LongValue);
        Assert.AreEqual(spo.TimeSpanValue, build.TimeSpanValue);
        Assert.AreEqual(spo.EnumValue, build.EnumValue);
    }

    [TestMethod]
    public void TestBuildAllEnitiesFromSimplePocoWithNullId()
    {
        SimplePoco spo = SimplePoco.CreatePocoWithoutID();
        IDictionary<string, object> allEntities = ObjectSerializer.Serialize(spo);
        Assert.AreEqual(9, allEntities.Count);
        Assert.IsFalse(allEntities.ContainsKey("Id"));
        TableEntity tableEntity = new TableEntity(allEntities);
        SimplePoco build = ObjectBuilder.Build<SimplePoco>(tableEntity);
        Assert.IsNotNull(build);
        Assert.IsNull(build.Id);
    }

    [TestMethod]
    public void TestBuildAllEnitiesFromParentPoco()
    {
        ParentPoco pp = ParentPoco.CreateParentWithChild();
        IDictionary<string, object> allEntities = ObjectSerializer.Serialize(pp);
        Assert.AreEqual(11, allEntities.Count);
        TableEntity tableEntity = new TableEntity(allEntities);
        ParentPoco build = ObjectBuilder.Build<ParentPoco>(tableEntity);
        Assert.IsNotNull(build);
        Assert.IsInstanceOfType(build, typeof(ParentPoco));
        Assert.IsNotNull(build.Child);
    }

    [TestMethod]
    public void TestBuildAllEnitiesFromMainWithParent()
    {
        MainWithParent mwp = MainWithParent.CreateMainWithParent();
        IDictionary<string, object> allEntities = ObjectSerializer.Serialize(mwp);
        Assert.AreEqual(22, allEntities.Count);
        Assert.IsTrue(allEntities.ContainsKey("Id"));
        TableEntity tableEntity = new TableEntity(allEntities);
        MainWithParent build = ObjectBuilder.Build<MainWithParent>(tableEntity);
        Assert.IsNotNull(build);
        Assert.IsInstanceOfType(build, typeof(MainWithParent));
    }

    [TestMethod]
    public void TestBuildAllEntitesFromBooleanAndByte()
    {
        ByteAndBooleanPoco spo = ByteAndBooleanPoco.Create();
        IDictionary<string, object> allEntities = ObjectSerializer.Serialize(spo);
        Assert.AreEqual(3, allEntities.Count);
        TableEntity tableEntity = new TableEntity(allEntities);
        ByteAndBooleanPoco build = ObjectBuilder.Build<ByteAndBooleanPoco>(tableEntity);
        Assert.IsNotNull(build);
        Assert.AreEqual(spo.BoolValue, build.BoolValue);
        Assert.AreEqual(spo.BooleanValue, build.BooleanValue);
        Assert.AreEqual(spo.ByteValue, build.ByteValue);
    }
}