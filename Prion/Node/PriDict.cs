// using Prion.Node.Converter;

using System.Collections;

namespace Prion.Node;
public class PriDict: PriNode, IEnumerable
{
    public readonly Dictionary<string,PriNode> Data;
    public override IEnumerable<PriNode> Keys => [..Data.Keys.Select(key => new PriString(key))];
    public override IEnumerable<PriNode> Values => Data.Values;
    public override IEnumerable<(PriNode, PriNode)> Entries => [..Data.Select(item => (new PriString(item.Key), item.Value))];
    public override int Count => Data.Count;
    // public PriDict(){}
    public PriDict(int capacity = 0)
    {
        Data = new(capacity);
    }
    public PriDict(Dictionary<string,PriNode> data)
    {
        Data = data;
    }
    public PriDict(IEnumerable<KeyValuePair<string, PriNode>> keyValuePairs)
    {
        Data = new(keyValuePairs);
    }
    public PriDict(IEnumerable<(string, PriNode)> keyValuePairs)
    {
        Data = new(keyValuePairs.Select(pair => new KeyValuePair<string, PriNode>(pair.Item1, pair.Item2)));
    }
    public override PriNodeKind Kind => PriNodeKind.Dict;
    public override bool IsImmutable => false;
    public void Add(string key, PriNode value)
    {
        Data.Add(key, value);
    }
    public override PriNode DeepCopy()
    {
        PriDict copy = new(Count);
        foreach (var (key, val) in Data)
        {
            copy.Add(key, val.DeepCopy());
        }
        return copy;
    }
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

    public IEnumerator GetEnumerator()
    {
        return (IEnumerator)Data;
    }
}