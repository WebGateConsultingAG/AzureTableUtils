namespace WebGate.Azure.TableUtils.Converter;

public interface IConverter
{
    public bool IsType(Type type);

    public string GetValue(Type type, object value);

    public object? BuildValue(string? value, Type type);
}