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
    public override IEnumerable<(string, PriNode)> GetEntries()
    {
        foreach (var (key,value) in Data)
        {
            yield return(key, value);
        }
    }
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