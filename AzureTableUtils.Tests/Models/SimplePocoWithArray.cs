namespace WebGate.Azure.TableUtils.Test;

public class SimplePocoWithArray
{
    public string? Id { set; get; }
    public int[]? IntArray { set; get; }

    public string[]? StringArray { set; get; }

    public DateTime[]? DateTimeArray { set; get; }

    public static SimplePocoWithArray CreateEmptySimplePocoWithArray()
    {
        SimplePocoWithArray spwa = new();
        return spwa;
    }

    public static SimplePocoWithArray CreateFilledSimplePocoWithArray()
    {
        SimplePocoWithArray spwa = new();
        int[] intArray = [0, 2, 19, 3, 1];
        spwa.IntArray = intArray;
        string[] stringArray = ["David", "Simon", "Sara", "Christian"];
        spwa.StringArray = stringArray;
        DateTime[] dtArray = [new(), new()];
        spwa.DateTimeArray = dtArray;
        return spwa;
    }
}