namespace Prion.Node;

public class PriVariant<T>: PriNode
{
    public readonly T Value;
    public override PriNodeKind Kind => PriNodeKind.Variant;
    public PriVariant(T value)
    {
        Value = value;
    }
    public override string ToString()
    {
        return Value?.ToString() ?? $"{typeof(T)}";
    }
    public override bool TryAs<U>(out U value)
    {
        if(base.TryAs(out value)) return true;
        if(this is PriVariant<U> variant)
        {
            value = variant.Value;
            return true;
        }
        return false;
    }
}