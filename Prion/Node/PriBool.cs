namespace Prion.Node;

public class PriBool: PriNode
{
    public readonly bool Value;
    public override PriNodeKind Kind => PriNodeKind.Bool;
    private PriBool(bool value)
    {
        Value = value;
    }
    public override string ToString()
    {
        return Value ? "true" : "false";
    }
    public static readonly PriBool True = new(true);
    public static readonly PriBool False = new(false);
    public override bool TryAs<T>(out T value)
    {
        if(base.TryAs(out value)) return true;
        return TryAsInternal(Value, out value);
        // if(value is bool b)
        // {
        //     Console.WriteLine($"jerkbag {b}");
        //     return TryAsInternal(b, out value);
        // }
        // return false;
    }
}