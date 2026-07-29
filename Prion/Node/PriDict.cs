// using Prion.Node.Converter;

namespace Prion.Node;
public class PriDict: PriNode
{
    public readonly Dictionary<string,PriNode> Data = [];
    public PriDict(){}
    public PriDict(Dictionary<string,PriNode> data)
    {
        Data = data;
    }

    public override PriNodeKind Kind => PriNodeKind.Dict;
    public override bool IsImmutable => false;
    public override bool TryGet<T>(string key, out T value)
    {
        value = default!;
        if(!Data.TryGetValue(key, out var val)) return false;
        return val.TryAs(out value);
    }
    public override bool TryGet<T>(int index, out T value)
    {
        return TryGet(index.ToString(), out value);
    }
    // public override PriNode Get(string key)
    // {
    //     if(!Data.TryGetValue(key, out var val)) return PriNull.Null;
    //     return val;
    // }
    // public override PriNode Get(int index)
    // {
    //     return Get(index.ToString());
    // }
    public override bool TrySet(string key, PriNode node)
    {
        Data[key] = node;
        return true;
    }
    public override bool TrySet(int index, PriNode node)
    {
        return TrySet(index.ToString(), node);
    }
    public override bool TryAdd(string key, PriNode node)
    {
        return Data.TryAdd(key, node);
    }
    // public bool TryGet<T>(string key, out T value)
    // {
    //     value = default!;
    //     if(!Data.TryGetValue(key, out var priNode)) return false;
    //     if(priNode is T val)
    //     {
    //         value = val;
    //         return true;
    //     }
    //     return PriNodeConverter.TryToValue(priNode, out value);
    // }
    // public bool TryGetList<T>(string key, out List<T> values)
    // {
    //     values = default!;
    //     if(!TryGet(key, out PriList priList)) return false;
    //     return priList.TryAs(out values);
    // }
    // public bool TryGetList<T>(string key, out T[] values)
    // {
    //     values = default!;
    //     if(!TryGet(key, out PriList priList)) return false;
    //     return priList.TryAs(out values);
    // }
    // public bool TrySet<T>(string key, T value)
    // {
    //     if(value is PriNode priNode){}
    //     else if(PriNodeConverter.TryToPrion(value, out priNode)){}
    //     else{}
    //     Data[key] = priNode;
    //     return true;
    // }
    // public bool TrySetList<T>(string key, IEnumerable<T> values)
    // {
    //     if(!PriList.TryFrom(values, out var priList)) return false;
    //     Data[key] = priList;
    //     return true;
    // }
    public override string ToString()
    {
        var sb = SbPool.Get();
        sb.Append('{');
        foreach (var (key, value) in Data)
        {
            sb.Append(key);
            sb.Append(": ");
            sb.Append(value.ToString());
            sb.Append(',');
        }
        sb.Append('}');
        return SbPool.Free(sb);
    }
}