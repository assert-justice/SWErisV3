using Prion.Node;

namespace Eris.Utils;

public class ErData
{
    public readonly string Name;
    private PriNode Node;
    public bool HasError{get; private set;}
    private readonly Queue<string> PathQueue = [];
    public ErData(string name, PriNode node)
    {
        Name = name;
        Node = node;
    }
    private bool LogError(params object?[] args)
    {
        HasError = true;
        return ErEngine.LogError(args);
    }
    private bool TrySplit(string path)
    {
        PathQueue.Clear();
        foreach (var str in path.Split('/', StringSplitOptions.TrimEntries))
        {
            if(string.IsNullOrEmpty(str)) return LogError("Invalid path, segments cannot be empty");
            PathQueue.Enqueue(str);
        }
        return true;
    }
    public bool TryGet<T>(string path, out T value)
    {
        value = default!;
        if(!TrySplit(path)) return false;
        return TryGet(Node, out value);
    }
    private bool TryGet<T>(PriNode node, out T value)
    {
        value = default!;
        if(PathQueue.TryDequeue(out string? seg))
        {
            if(seg is null) throw new("Should be unreachable");
            if(!node.TryGet(seg, out PriNode next)) return LogError("Invalid path, segment '", seg, "' not found.");
            return TryGet(next, out value);
        }
        return Node.TryAs(out value);
    }
    // public bool TrySet<T>(string path, T value) where T : PriNode
    // {
    //     if(!TrySplit(path)) return false;
    //     if()
    //     return TrySet(null, Node, value);
    // }
    // private bool TrySet<T>(PriNode? parent, PriNode node, T value)
    // {
    //     if(PathQueue.TryDequeue(out string? seg))
    //     {
    //         if(seg is null) throw new("Should be unreachable");
    //         if(node.TryGet(seg, out PriNode next)) return TryGet(next, out value);
    //         if(node is PriDict dict)
    //         {
    //             dict.TrySet(seg, )
    //         }
    //     }
    // }
}