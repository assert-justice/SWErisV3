using Eris;

namespace SpoonWitch.Game.Map.MapObject;

public class SwMapObjectLookup
{
    private readonly Dictionary<string,SwMapObject> MapObjects = [];
    private readonly Dictionary<Type,Dictionary<Type,SwMapObject>> TypeLookup = [];
    public void AddObject(SwMapObject mapObject)
    {
        if(!MapObjects.TryAdd(mapObject.Id, mapObject))
        {
            ErEngine.LogWarning("a map object with id ", mapObject.Id, " already exists");
            return;
        }
        var type = mapObject.GetType();
        if(!TypeLookup.TryGetValue(type, out var lookup))
        {
            lookup = [];
            TypeLookup.Add(type, lookup);
        }
        lookup.Add(mapObject.GetType(), mapObject);
    }
    public bool TryGetObject(string id, out SwMapObject mapObject)
    {
        return MapObjects.TryGetValue(id, out mapObject!);
    }
    public bool TryGetObject<T>(string id, out T mapObject) where T: SwMapObject
    {
        mapObject = default!;
        if(!TryGetObject(id, out var obj)) return false;
        if(obj is not T t) return ErEngine.LogWarning("a map object with id '", id, "' exists but has the wrong type, expected ", typeof(T), " but recieved ", obj.GetType());
        mapObject = t;
        return true;
    }
    public IEnumerable<SwMapObject> GetObjects()
    {
        foreach (var item in MapObjects.Values)
        {
            yield return item;
        }
    }
    public IEnumerable<T> GetObjects<T>() where T: SwMapObject
    {
        if(!TypeLookup.TryGetValue(typeof(T), out var objects)) yield break;
        foreach (var item in objects.Values)
        {
            if(item is not T t) throw new("should be unreachable");
            yield return t;
        }
    }
}