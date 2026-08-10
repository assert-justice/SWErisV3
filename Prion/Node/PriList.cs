namespace Prion.Node;
public class PriList: PriNode
{
    public readonly List<PriNode> Data;
    public PriList()
    {
        Data = [];
    }
    public PriList(List<PriNode> values)
    {
        Data = values;
    }
    public PriList(int capacity)
    {
        Data = new(capacity);
    }
    public override PriNodeKind Kind => PriNodeKind.List;
    public override bool IsImmutable => false;
    public override int Count => Data.Count;
    public override IEnumerable<PriNode> Keys => Enumerable.Range(0, Count).Select(i => new PriNumber(i));
    public override IEnumerable<PriNode> Values => Data;
    public override IEnumerable<(PriNode, PriNode)> Entries => [..Data.Select((node,idx)=> (new PriNumber(idx), node))];
    public override bool TryGet<T>(string key, out T value)
    {
        value = default!;
        if(!int.TryParse(key, out int idx)) return false;
        return TryGet(idx, out value);
    }
    public override bool TryGet<T>(int index, out T value)
    {
         value = default!;
        if(index < 0) index += Data.Count;
        if(index < 0 || index >= Data.Count) return false;
        return Data[index].TryAs(out value);
    }
    public override bool TrySet(string key, PriNode node)
    {
        if(!int.TryParse(key, out int idx)) return false;
        return TrySet(idx, node);
    }
    public override bool TrySet(int index, PriNode node)
    {
        if(index < 0 || index >= Data.Count) return false;
        Data[index] = node;
        return true;
    }
    public override string ToString()
    {
        var sb = SbPool.Get();
        sb.Append('[');
        foreach (var item in Values)
        {
            sb.Append(item.ToString());
            sb.Append(',');
        }
        sb.Append(']');
        return SbPool.Free(sb);
    }
}