namespace Prion.Node;

public class PriString: PriNode
{
    public readonly string Value;
    public override PriNodeKind Kind => PriNodeKind.String;
    public PriString(string value)
    {
        Value = value;
    }
    public override int Count => Value.Length;
    // public override bool TryAsEnum<TEnum>(out TEnum value)
    // {
    //     return Enum.TryParse(Value, out value);
    // }
    public override string ToString()
    {
        return $"\"{Value}\"";
    }
    public override bool TryAs<T>(out T value)
    {
        if(base.TryAs(out value)) return true;
        if(typeof(T) == typeof(string))
        // if(value is string str)
        {
            return TryAsInternal(Value, out value);
        }
        return false;
    }
}