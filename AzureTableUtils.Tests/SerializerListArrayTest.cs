using Newtonsoft.Json;

namespace WebGate.Azure.TableUtils.Test;

[TestClass]
public class SerializerListArrayTest
{
    [TestMethod]
    public void TestPocoWithEmptyArray()
    {
        SimplePocoWithArray spwa = SimplePocoWithArray.CreateEmptySimplePocoWithArray();
        IDictionary<string, object> allEntities = ObjectSerializer.Serialize(spwa);
        Assert.AreEqual(0, allEntities.Count);
    }

    [TestMethod]
    public void TestPocoWithInitializedArray()
    {
        SimplePocoWithArray spwa = SimplePocoWithArray.CreateFilledSimplePocoWithArray();
        string jsonArray = JsonConvert.SerializeObject(spwa.DateTimeArray);
        IDictionary<string, object> allEntities = ObjectSerializer.Serialize(spwa);
        Assert.AreEqual(3, allEntities.Count);
        Assert.AreEqual(jsonArray, allEntities["DateTimeArray"]);
    }
}