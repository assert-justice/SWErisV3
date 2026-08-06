using System.Text;
using Prion.Utils;

namespace Prion.Node;

public enum PriNodeKind
{
    Bool,
    Dict,
    Error,
    List,
    Null,
    Number,
    String,
    Variant,
}
public abstract class PriNode
{
    // private static readonly Dictionary<Type,()>
    protected static readonly StringBuilder Sb = new();
    protected static readonly PriSbPool SbPool = new();
    public abstract PriNodeKind Kind{get;}
    public virtual bool IsError{get => false;}
    public virtual bool IsImmutable{get => true;}
    public virtual bool TryAs<T>(out T value)
    {
        if(this is T val)
        {
            value = val;
            return true;
        }
        value = default!;
        return false;
    }
    public virtual bool TryAsList<T>(out List<T> values)
    {
        values = null!;
        return false;
    }
    public virtual bool TryGet<T>(string key, out T value)
    {
        value = default!;
        return false;
    }
    public virtual bool TryGet<T>(int index, out T value)
    {
        value = default!;
        return false;
    }
    public PriNode Get(string key)
    {
        if(TryGet(key, out PriNode value)) return value;
        return PriNull.Null;
    }
    public PriNode Get(int index)
    {
        if(TryGet(index, out PriNode value)) return value;
        return PriNull.Null;
    }
    public virtual IEnumerable<(string,PriNode)> GetEntries()
    {
        yield break;
    }
    public virtual bool TrySet(string key, PriNode node){return false;}
    public virtual bool TrySet(int index, PriNode node){return false;}
    public virtual bool TryAdd(string key, PriNode node){return false;}
    public virtual bool TryAdd(PriNode node){return false;}
    protected static bool TryAsInternal<T, U>(U input, out T value)
    {
        value = default!;
        if(input is not T val) return false;
        value = val;
        return true;
    }
}