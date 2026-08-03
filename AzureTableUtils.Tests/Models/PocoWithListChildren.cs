namespace WebGate.Azure.TableUtils.Test;

public class PocoWihtListChildren
{
    public string? Id { get; set; }

    public List<SimplePoco>? Children { set; get; }

    public static PocoWihtListChildren CreateInitializdedPWLC()
    {
        PocoWihtListChildren pwlc = new PocoWihtListChildren
        {
            Id = "0178301",
            Children = new List<SimplePoco>()
        };
        for (int i = 0; i < 3; i++)
        {
            pwlc.Children.Add(SimplePoco.CreateInitializedPoco());
        }
        return pwlc;
    }
}