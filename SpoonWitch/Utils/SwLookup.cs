namespace SpoonWitch.Utils;

public class SwLookup
{
    private readonly Dictionary<string, object> Lookup = [];
    private readonly Dictionary<Type,Dictionary<string, object>> TypeLookup = [];
    public bool TryAdd(string id, object obj)
    {
        if(!Lookup.TryAdd(id, obj)) return false;
        var type = obj.GetType();
        if(!TypeLookup.TryGetValue(type, out var lookup))
        {
            lookup = [];
            TypeLookup[type] = lookup;
        }
        if(!lookup.TryAdd(id, obj)) throw new("should be unreachable");
        return true;
    }
    public bool TryGet<T>(string id, out T obj)
    {
        obj = default!;
        if(!Lookup.TryGetValue(id, out var o)) return false;
        if(o is not T val) return false;
        obj = val;
        return true;
    }
    public IEnumerable<T> GetAllValues<T>()
    {
        foreach (var item in Lookup.Values)
        {
            if(item is not T val) throw new("should be unreachable");
            yield return val;
        }
    }
    public IEnumerable<T> GetValues<T>()
    {
        if(!TypeLookup.TryGetValue(typeof(T), out var lookup)) yield break;
        foreach (var item in lookup.Values)
        {
            if(item is not T val) throw new("should be unreachable");
            yield return val;
        }
    }
    public bool Remove(string id)
    {
        if(!Lookup.TryGetValue(id, out var obj)) return false;
        var type = obj.GetType();
        if(!TypeLookup.TryGetValue(type, out var lookup)) throw new("should be unreachable");
        lookup.Remove(id);
        Lookup.Remove(id);
        return true;
    }
}
