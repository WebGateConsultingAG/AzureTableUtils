using System.Text;

namespace WebGate.Azure.TableUtils.Test;

public class ByteAndBooleanPoco
{
    public bool BoolValue { set; get; }

    public Boolean BooleanValue { set; get; }

    public byte[]? ByteValue { set; get; }

    public static ByteAndBooleanPoco Create()
    {
        ByteAndBooleanPoco bbp = new ByteAndBooleanPoco
        {
            BooleanValue = true,
            BoolValue = true,
            ByteValue = Encoding.UTF8.GetBytes("HELLO WORLD")
        };
        return bbp;
    }
}