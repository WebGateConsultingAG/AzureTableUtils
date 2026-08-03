namespace WebGate.Azure.TableUtils.Test;

public class MainWithParent
{
    public string? Id { set; get; }
    public ParentPoco? Parent { set; get; }

    public SimplePoco? Child { set; get; }

    public static MainWithParent CreateMainWithParent()
    {
        MainWithParent mwp = new MainWithParent
        {
            Child = SimplePoco.CreateInitializedPoco(),
            Parent = ParentPoco.CreateParentWithChild(),
            Id = "01820281"
        };
        return mwp;
    }

    public override bool Equals(object? other)
    {
        if ((other == null) || !this.GetType().Equals(other.GetType()))
        {
            return false;
        }
        else
        {
            MainWithParent po = (MainWithParent)other;
            return po.GetHashCode() == this.GetHashCode();
        }
    }

    public override int GetHashCode()
    {
        return (Id != null ? Id.GetHashCode() : 0) + (Child != null ? Child.GetHashCode() : 0) + (Parent != null ? Parent.GetHashCode() : 0);
    }
}