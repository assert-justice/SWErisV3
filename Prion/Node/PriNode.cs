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
    // private static readonly Dictionary<Type,Func<object, PriNode>> Converters;
    // static PriNode()
    // {

    //     (Type,Func<object,PriNode>)[] converters = [
    //         // Todo: make truthy method for bool
    //         // (typeof(bool), (o)=> (o is bool b && b) ? PriBool.True: PriBool.False),
    //         (typeof(string), (o) => o is string s ? new PriString(s) : PriNull.Null),
    //         (typeof(int), (o) => o is int s ? new PriNumber(s) : PriNull.Null),
    //     ];
    //     Converters = new(converters.Length);
    //     foreach (var (key,value) in converters)
    //     {
    //         Converters.Add(key, value);
    //     }
    // }
    private static readonly Queue<object> ObjectQueue = [];
    protected static readonly StringBuilder Sb = new();
    protected static readonly PriSbPool SbPool = new();
    public abstract PriNodeKind Kind{get;}
    public virtual bool IsError => false;
    public virtual bool IsImmutable => true;
    public virtual bool IsTruthy => Count == 0;
    public virtual int Count => 0;
    public virtual IEnumerable<PriNode> Keys{get{yield break;}}
    public virtual IEnumerable<PriNode> Values{get{yield break;}}
    public virtual IEnumerable<(PriNode,PriNode)> Entries{get{yield break;}}
    public virtual PriNode DeepCopy()
    {
        return this;
    }
    public virtual bool TryAs<T>(out T value)
    {
        if(this is T val)
        {
            value = val;
            return true;
        }
        return PriConverter.TryFromPri(this, out value);
    }
    private static IEnumerable<T> DrainQueue<T>()
    {
        while(ObjectQueue.TryDequeue(out var obj))
        {
            if(obj is not T v) throw new("should be unreachable");
            yield return v;
        }
    }
    private static bool TryEnqueue<T>(IEnumerable<PriNode> nodes)
    {
        ObjectQueue.Clear();
        foreach (var item in nodes)
        {
            if(!item.TryAs(out T value) || value is null)
            {
                ObjectQueue.Clear();
                return false;
            }
            ObjectQueue.Enqueue(value);
        }
        return true;
    }
    public bool ValuesTryAs<T>(out IEnumerable<T> values)
    {
        values = default!;
        if(!TryEnqueue<T>(Values)) return false;
        values = DrainQueue<T>();
        return true;
    }
    // public virtual bool TryAsList<T>(out List<T> values)
    // {
    //     values = null!;
    //     return false;
    // }
    // public virtual bool TryAsEnum<TEnum>(out TEnum value) where TEnum: struct, Enum
    // {
    //     value = default;
    //     return false;
    // }
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
    public static bool TryToPrion<T>(T value, out PriNode priNode)
    {
        if(value is not PriNode node) return PriConverter.TryToPri(value, out priNode);
        priNode = node;
        return true;
    }
    public static bool TryToPrion<T,U>(T value, out U priNode) where U: PriNode
    {
        return PriConverter.TryToPri(value, out priNode);
    }
    public virtual bool TrySet(string key, PriNode node){return false;}
    public bool TrySet<T>(string key, T value)
    {
        if(value is PriNode priNode) return TrySet(key, priNode);
        if(!TryToPrion(value, out priNode)) return false;
        return TrySet(key, priNode);
    }
    public virtual bool TrySet(int index, PriNode node){return false;}
    public virtual bool TrySet<T>(int index, T value)
    {
        if(value is PriNode priNode) return TrySet(index, priNode);
        if(!TryToPrion(value, out priNode)) return false;
        return TrySet(index, priNode);
    }
    protected static bool TryAsInternal<T, U>(U input, out T value)
    {
        value = default!;
        if(input is not T val) return false;
        value = val;
        return true;
    }
}