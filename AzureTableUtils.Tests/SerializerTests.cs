namespace WebGate.Azure.TableUtils.Test;

[TestClass]
public class SerlializerTests
{
    [TestMethod]
    public void TestExtractAllEnitiesFromSimplePoco()
    {
        SimplePoco spo = SimplePoco.CreateInitializedPoco();
        IDictionary<string, object> allEntities = ObjectSerializer.Serialize(spo);
        Assert.AreEqual(10, allEntities.Count);
        //CHECK ID
        Assert.IsTrue(allEntities.ContainsKey("Id"));
        Assert.AreEqual(allEntities["Id"], spo.Id);

        Assert.IsTrue(allEntities.ContainsKey("IntValue"));
        Assert.AreEqual(allEntities["IntValue"], spo.IntValue);

        Assert.IsTrue(allEntities.ContainsKey("LongValue"));
        Assert.AreEqual(allEntities["LongValue"], spo.LongValue);

        Assert.IsTrue(allEntities.ContainsKey("DoubleValue"));
        Assert.AreEqual(allEntities["DoubleValue"], spo.DoubleValue);

        Assert.IsTrue(allEntities.ContainsKey("DecimalValue"));
        Assert.AreEqual(allEntities["DecimalValue"], spo.DecimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture));

        Assert.IsTrue(allEntities.ContainsKey("GuidValue"));
        Assert.AreEqual(allEntities["GuidValue"], spo.GuidValue);

        Assert.IsTrue(allEntities.ContainsKey("DTValue"));
        Assert.AreEqual(allEntities["DTValue"], spo.DTValue);

        Assert.IsTrue(allEntities.ContainsKey("DTOValue"));
        Assert.AreEqual(allEntities["DTOValue"], spo.DTOValue);

        Assert.IsTrue(allEntities.ContainsKey("EnumValue"));
        Assert.AreEqual(allEntities["EnumValue"], spo.EnumValue.ToString());
    }

    [TestMethod]
    public void TestExtractAllEnitiesFromSimplePocoWithNullId()
    {
        SimplePoco spo = SimplePoco.CreatePocoWithoutID();
        IDictionary<string, object> allEntities = ObjectSerializer.Serialize(spo);
        Assert.AreEqual(9, allEntities.Count);
        Assert.IsFalse(allEntities.ContainsKey("Id"));
    }

    [TestMethod]
    public void TestExtractAllEnitiesFromParentPoco()
    {
        ParentPoco pp = ParentPoco.CreateParentWithChild();
        IDictionary<string, object> allEntities = ObjectSerializer.Serialize(pp);
        Assert.IsNotNull(pp.Child);
        Assert.AreEqual(11, allEntities.Count);
        Assert.IsTrue(allEntities.ContainsKey("Id"));

        Assert.IsTrue(allEntities.ContainsKey("Child_Id"));
        Assert.AreEqual(allEntities["Child_Id"], pp.Child.Id);

        Assert.IsTrue(allEntities.ContainsKey("Child_IntValue"));
        Assert.AreEqual(allEntities["Child_IntValue"], pp.Child.IntValue);

        Assert.IsTrue(allEntities.ContainsKey("Child_LongValue"));
        Assert.AreEqual(allEntities["Child_LongValue"], pp.Child.LongValue);

        Assert.IsTrue(allEntities.ContainsKey("Child_DoubleValue"));
        Assert.AreEqual(allEntities["Child_DoubleValue"], pp.Child.DoubleValue);

        Assert.IsTrue(allEntities.ContainsKey("Child_GuidValue"));
        Assert.AreEqual(allEntities["Child_GuidValue"], pp.Child.GuidValue);

        Assert.IsTrue(allEntities.ContainsKey("Child_DTValue"));
        Assert.AreEqual(allEntities["Child_DTValue"], pp.Child.DTValue);

        Assert.IsTrue(allEntities.ContainsKey("Child_DTOValue"));
        Assert.AreEqual(allEntities["Child_DTOValue"], pp.Child.DTOValue);
    }

    [TestMethod]
    public void TestExtractAllEnitiesFromMainWithParent()
    {
        MainWithParent mwp = MainWithParent.CreateMainWithParent();
        IDictionary<string, object> allEntities = ObjectSerializer.Serialize(mwp);
        Assert.IsNotNull(mwp.Child);
        Assert.IsNotNull(mwp.Parent);
        Assert.IsNotNull(mwp.Parent.Child);

        Assert.AreEqual(22, allEntities.Count);
        Assert.IsTrue(allEntities.ContainsKey("Id"));

        Assert.IsTrue(allEntities.ContainsKey("Child_Id"));
        Assert.AreEqual(allEntities["Child_Id"], mwp.Child.Id);

        Assert.IsTrue(allEntities.ContainsKey("Child_IntValue"));
        Assert.AreEqual(allEntities["Child_IntValue"], mwp.Child.IntValue);

        Assert.IsTrue(allEntities.ContainsKey("Child_LongValue"));
        Assert.AreEqual(allEntities["Child_LongValue"], mwp.Child.LongValue);

        Assert.IsTrue(allEntities.ContainsKey("Child_DoubleValue"));
        Assert.AreEqual(allEntities["Child_DoubleValue"], mwp.Child.DoubleValue);

        Assert.IsTrue(allEntities.ContainsKey("Child_GuidValue"));
        Assert.AreEqual(allEntities["Child_GuidValue"], mwp.Child.GuidValue);

        Assert.IsTrue(allEntities.ContainsKey("Child_DTValue"));
        Assert.AreEqual(allEntities["Child_DTValue"], mwp.Child.DTValue);

        Assert.IsTrue(allEntities.ContainsKey("Child_DTOValue"));
        Assert.AreEqual(allEntities["Child_DTOValue"], mwp.Child.DTOValue);

        //CHECK PARENT
        Assert.IsTrue(allEntities.ContainsKey("Parent_Id"));
        Assert.AreEqual(allEntities["Parent_Id"], mwp.Parent.Id);

        Assert.IsTrue(allEntities.ContainsKey("Parent_Child_IntValue"));
        Assert.AreEqual(allEntities["Child_IntValue"], mwp.Parent.Child.IntValue);

        Assert.IsTrue(allEntities.ContainsKey("Parent_Child_LongValue"));
        Assert.AreEqual(allEntities["Child_LongValue"], mwp.Parent.Child.LongValue);

        Assert.IsTrue(allEntities.ContainsKey("Parent_Child_DoubleValue"));
        Assert.AreEqual(allEntities["Child_DoubleValue"], mwp.Parent.Child.DoubleValue);

        Assert.IsTrue(allEntities.ContainsKey("Parent_Child_GuidValue"));
        Assert.AreEqual(allEntities["Parent_Child_GuidValue"], mwp.Parent.Child.GuidValue);

        Assert.IsTrue(allEntities.ContainsKey("Parent_Child_DTValue"));
        Assert.AreEqual(allEntities["Parent_Child_DTValue"], mwp.Parent.Child.DTValue);

        Assert.IsTrue(allEntities.ContainsKey("Parent_Child_DTOValue"));
        Assert.AreEqual(allEntities["Parent_Child_DTOValue"], mwp.Parent.Child.DTOValue);
    }

    [TestMethod]
    public void TestBooleanAndByte()
    {
        ByteAndBooleanPoco spo = ByteAndBooleanPoco.Create();
        IDictionary<string, object> allEntities = ObjectSerializer.Serialize(spo);
        Assert.AreEqual(3, allEntities.Count);
        //CHECK ID
        Assert.IsTrue(allEntities.ContainsKey("BoolValue"));
        Assert.AreEqual(allEntities["BoolValue"], spo.BoolValue);

        Assert.IsTrue(allEntities.ContainsKey("BooleanValue"));
        Assert.AreEqual(allEntities["BooleanValue"], spo.BooleanValue);

        Assert.IsTrue(allEntities.ContainsKey("ByteValue"));
        Assert.AreEqual(allEntities["ByteValue"], spo.ByteValue);
    }
}