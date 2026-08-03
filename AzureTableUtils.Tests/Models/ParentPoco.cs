namespace WebGate.Azure.TableUtils.Test;

public class ParentPoco
{
    public string? Id { get; set; }
    public SimplePoco? Child { set; get; }

    public static ParentPoco CreateParentWithChild()
    {
        ParentPoco pp = new ParentPoco
        {
            Child = SimplePoco.CreateInitializedPoco(),
            Id = "02830128101"
        };
        return pp;
    }

    public override bool Equals(object? other)
    {
        if ((other == null) || !this.GetType().Equals(other.GetType()))
        {
            return false;
        }
        else
        {
            ParentPoco po = (ParentPoco)other;
            return po.GetHashCode() == this.GetHashCode();
        }
    }

    public override int GetHashCode()
    {
        return (Id != null ? Id.GetHashCode() : 0) + (Child != null ? Child.GetHashCode() : 0);
    }
}