using Prion.Node;

namespace Prion.Validator;

public interface IPriSchema<T>
{
    public static abstract bool TryFromPrion(PriNode priNode, out T value);
    public abstract PriNode ToPrion();
}