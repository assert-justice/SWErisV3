using System.Reflection.Metadata;
// using Prion.Node.Converter;

namespace Prion.Node;
public class PriList: PriNode
{
    public readonly List<PriNode> Values;
    public PriList()
    {
        Values = [];
    }
    public PriList(List<PriNode> values)
    {
        Values = values;
    }
    public PriList(int capacity)
    {
        Values = new(capacity);
    }
    public override PriNodeKind Kind => PriNodeKind.List;
    public override bool IsImmutable => false;
    public override bool TryAsList<T>(out List<T> values)
    {
        values = new(Values.Count);
        foreach (var v in Values)
        {
            if(!v.TryAs(out T val))
            {
                values = null!;
                return false;
            }
            else values.Add(val);
        }
        return true;
    }
    public override bool TryGet<T>(string key, out T value)
    {
        // return base.TryGet(key, out value);
        value = default!;
        if(!int.TryParse(key, out int idx)) return false;
        return TryGet(idx, out value);
    }
    public override bool TryGet<T>(int index, out T value)
    {
         value = default!;
        if(index < 0) index += Values.Count;
        if(index < 0 || index >= Values.Count) return false;
        return Values[index].TryAs(out value);
    }
    public override IEnumerable<(string, PriNode)> GetEntries()
    {
        for (int idx = 0; idx < Values.Count; idx++)
        {
            yield return (idx.ToString(),Values[idx]);
        }
    }
    // public override PriNode Get(string key)
    // {
    //     if(!int.TryParse(key, out int idx)) return PriNull.Null;
    //     return Get(idx);
    // }
    // public override PriNode Get(int index)
    // {
    //     // Note: handles wrapping, so -1 is the end of the list
    //     if(index < 0) index += Values.Count;
    //     if(index < 0 || index >= Values.Count) return PriNull.Null;
    //     return Values[index];
    // }
    public override bool TrySet(string key, PriNode node)
    {
        if(!int.TryParse(key, out int idx)) return false;
        return TrySet(idx, node);
    }
    public override bool TrySet(int index, PriNode node)
    {
        if(index < 0 || index >= Values.Count) return false;
        Values[index] = node;
        return true;
    }
    public override bool TryAdd(PriNode node)
    {
       Values.Add(node);
       return true;
    }
    // public bool TryGet<T>(int idx, out T value)
    // {
    //     value = default!;
    //     if(idx < 0 || idx >= Values.Count) return false;
    //     var priNode = Values[idx];
    //     if(priNode is T val)
    //     {
    //         value = val;
    //         return true;
    //     }
    //     return PriNodeConverter.TryToValue(priNode, out value);
    // }
    // public bool TrySet<T>(int idx, T value)
    // {
    //     if(idx < 0 || idx >= Values.Count) return false;
    //     if(value is PriNode priNode){}
    //     else if(PriNodeConverter.TryToPrion(value, out priNode)){}
    //     else return false;
    //     Values[idx] = priNode;
    //     return true;
    // }
    // public bool TryAdd<T>(T value)
    // {
    //     if(value is PriNode priNode){}
    //     // {
    //     //     Values.Add(priNode);
    //     //     return true;
    //     // }
    //     else if(PriNodeConverter.TryToPrion(value, out priNode)){}
    //     else return false;
    //     Values.Add(priNode);
    //     return true;
    // }
    // private IEnumerable<T> GetValues<T>()
    // {
    //     foreach (var item in Values)
    //     {
    //         if(item is T val) yield return val;
    //         else if(PriNodeConverter.TryToValue(item, out val)) yield return val;
    //         else yield break;
    //     }
    // }
    // public bool TryAs<T>(out List<T> values)
    // {
    //     values = [..GetValues<T>()];
    //     if(values.Count == Values.Count) return true;
    //     values.Clear();
    //     return false;
    // }
    // public bool TryAs<T>(out T[] values)
    // {
    //     values = [..GetValues<T>()];
    //     if(values.Length == Values.Count) return true;
    //     values = [];
    //     return false;
    // }
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
    // public static bool TryFrom<T>(IEnumerable<T> values, out PriList priList)
    // {
    //     if(values.TryGetNonEnumeratedCount(out int count)) priList = new(count);
    //     else priList = new();
    //     if(!PriNodeConverter.TryGetConverter(typeof(T), out var converter)) return false;
    //     foreach (var item in values)
    //     {
    //         converter.TryToPrion(item, out var priNode);
    //         priList.Values.Add(priNode);
    //     }
    //     return true;
    // }
}