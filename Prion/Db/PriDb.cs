using Prion.Node;

namespace Prion.Db;

public class PriDb
{
    private PriNode Data = PriNull.Null;
    public PriDb(){}
    public PriDb(PriNode data)
    {
        Data = data;
    }
    private static Queue<string> SplitPath(string path)
    {
        // Todo: optimize this
        Queue<string> p = [];
        foreach (var item in path.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            p.Enqueue(item);
        }
        return p;
    }
    public bool TryMerge(string path, PriNode node)
    {
        if(node is PriDict || node is PriList)
        {
            foreach (var (key,val) in node.Entries)
            {
                if(!TryMerge(path + '/' + key, val)) return false;
            }
            return true;
        }
        else
        {
            return TrySet(path, node);
        }
    }
    public bool TryGet<T>(string path, out T value)
    {
        return TryGet(SplitPath(path), out value);
    }
    private bool TryGet<T>(Queue<string> path, out T value)
    {
        value = default!;
        PriNode? lastNode = null;
        string? lastKey = null;
        PriNode node = Data;
        while(path.TryDequeue(out string? key))
        {
            if(string.IsNullOrEmpty(key)) return false;
            if(node is PriNull)
            {
                node = new PriDict();
                if(lastNode is null || lastKey is null)
                {
                    Data = node;
                }
                else if (lastNode.TrySet(lastKey, node)){}
                else
                {
                    throw new("should be unreachable");
                }
                lastNode = node;
                lastKey = key;
                PriNode nextNode = new PriDict();
                node.TrySet(key, nextNode);
                node = nextNode;
            }
            else
            {
                lastNode = node;
                lastKey = key;
                node = node.Get(key);
            }
        }
        return node.TryAs(out value);
    }
    public bool TrySet(string path, PriNode value)
    {
        return TrySet(SplitPath(path), value);
    }
    public bool TrySet(Queue<string> path, PriNode value)
    {
        PriNode? lastNode = null;
        string? lastKey = null;
        PriNode node = Data;
        while(path.TryDequeue(out string? key))
        {
            if(string.IsNullOrEmpty(key)) return false;
            lastNode = node;
            lastKey = key;
            node = node.Get(key);
            if(node is PriNull)
            {
                node = new PriDict();
                lastNode.TrySet(key, node);
            }
        }
        if(lastNode is null || lastKey is null) Data = value;
        else lastNode.TrySet(lastKey, value);
        return true;
    }
    public override string ToString()
    {
        return Data.ToString() ?? string.Empty;
    }
}