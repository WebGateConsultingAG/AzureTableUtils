namespace WebGate.Azure.TableUtils.Test;

public class SimplePocoPart
{
    public SP EnumValue { set; get; }
}

public enum SP
{
    VALID,
    INVALID
}

public class SimplePoco
{
    public string? Id { set; get; }
    public int IntValue { set; get; }

    public long LongValue { set; get; }

    public double DoubleValue { set; get; }

    public Guid GuidValue { set; get; }

    public DateTime DTValue { set; get; }

    public DateTimeOffset DTOValue { set; get; }

    public TimeSpan TimeSpanValue { set; get; }

    public SP EnumValue { set; get; }

    public static SimplePoco CreateInitializedPoco()
    {
        SimplePoco simplePoco = new SimplePoco
        {
            Id = "02381012",
            DoubleValue = 2018101.00812,
            IntValue = 9789677,
            DTOValue = DateTimeOffset.Now,
            LongValue = 100008937819,
            GuidValue = Guid.NewGuid(),
            DTValue = DateTime.UtcNow,
            TimeSpanValue = new TimeSpan(5, 3, 12),
            EnumValue = SP.INVALID
        };
        return simplePoco;
    }

    public static SimplePoco CreatePocoWithoutID()
    {
        SimplePoco sp = CreateInitializedPoco();
        sp.Id = null;
        return sp;
    }

    public override bool Equals(object? obj)
    {
        //Check for null and compare run-time types.
        if ((obj == null) || !this.GetType().Equals(obj.GetType()))
        {
            return false;
        }
        else
        {
            SimplePoco spo = (SimplePoco)obj;
            return (Id == spo.Id) && (EnumValue == spo.EnumValue) && (spo.DoubleValue == DoubleValue) && (spo.DTOValue == DTOValue) && (spo.DTValue == DTValue) && (spo.GuidValue == GuidValue);
        }
    }

    public override int GetHashCode()
    {
        return (Id != null ? Id.GetHashCode() : 0) + EnumValue.GetHashCode();
    }
}