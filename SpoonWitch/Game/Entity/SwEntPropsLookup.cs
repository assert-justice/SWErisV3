using Eris;

namespace SpoonWitch.Game.Entity;

public class SwEntPropsLookup
{
    private readonly Dictionary<int,SwEntPropsBase> EntProps = [];
    private readonly Dictionary<Type,Dictionary<int,SwEntPropsBase>> TypeLookup = [];
    public void AddEntProps<T>(SwEntProps<T> entProps) where T: SwEntity, ISwEntity<T>
    {
        if(!EntProps.TryAdd(entProps.Id, entProps))
        {
            ErEngine.LogWarning("an ent props entry for id ", entProps.Id, " already exists");
            return;
        }
        var type = typeof(T);
        if(!TypeLookup.TryGetValue(type, out var props))
        {
            props = [];
            TypeLookup[type] = props;
        }
        props[entProps.Id] = entProps;
    }
    public bool RemoveEntProps(SwEntity entity)
    {
        bool res = EntProps.Remove(entity.Id);
        if(res && TypeLookup.TryGetValue(entity.GetType(), out var lookup)) lookup.Remove(entity.Id);
        return res;
    }
    public bool TryGet(int id, out SwEntPropsBase entProps)
    {
        return EntProps.TryGetValue(id, out entProps!);
    }
    public bool TryGet<T>(int id, out SwEntProps<T> entProps) where T: SwEntity, ISwEntity<T>
    {
        entProps = default!;
        if(!TryGet(id, out var p)) return false;
        if(p is not SwEntProps<T> props) return ErEngine.LogWarning("a ent props entry with id '", id, "' exists but has the wrong type, expected ", typeof(T), " but recieved ", p.GetType());
        entProps = props;
        return true;
    }
    public IEnumerable<SwEntPropsBase> GetValues()
    {
        foreach (var item in EntProps.Values)
        {
            yield return item;
        }
    }
    public IEnumerable<SwEntProps<T>> GetValues<T>() where T: SwEntity, ISwEntity<T>
    {
        if(!TypeLookup.TryGetValue(typeof(T), out var lookup)) yield break;
        foreach (var item in lookup.Values)
        {
            if(item is not SwEntProps<T> val) throw new("should be unreachable");
            yield return val;
        }
    }
}