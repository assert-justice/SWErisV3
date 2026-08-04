using Eris;
using ErisMath;
using Prion.Node;

namespace SpoonWitch.Game.Map.MapObject;

public abstract class SwMapObject
{
    public readonly SwRoom Room;
    public readonly string Id;
    public readonly string Type;
    public readonly ErRect2I Rect;
    public readonly PriNode Fields;
    public bool IsGlobal => Fields.TryGet("is_global", out bool isGlobal) && isGlobal;
    public SwMapObject(SwRoom room, PriNode data)
    {
        Room = room;
        if(!data.Get("__identifier").TryAs(out Type)) throw new("no type");
        if(!data.Get("iid").TryAs(out Id)) throw new("no id");
        if(!data.Get("__worldX").TryAs(out int xPx)) throw new("no world x");
        if(!data.Get("__worldY").TryAs(out int yPx)) throw new("no world y");
        if(!data.Get("width").TryAs(out int widthPx)) throw new("no width");
        if(!data.Get("height").TryAs(out int heightPx)) throw new("no height");
        Rect = new(xPx, yPx, widthPx, heightPx);
        if(!data.Get("fieldInstances").TryAs(out PriList fieldList)) throw new("no fields");
        Fields = new PriDict();
        foreach (var item in fieldList.Values)
        {
            if(!item.Get("__identifier").TryAs(out string key)) throw new("no field name");
            var value = item.Get("__value");
            Fields.TrySet(key, value);
        }
    }
    public virtual void Update(){}
    public static bool TryFromData(SwRoom room, PriNode data, out SwMapObject mapObject)
    {
        mapObject = null!;
        if(!data.Get("__identifier").TryAs(out string type)) return false;
        try
        {
            switch (type)
            {
                case "area":
                    mapObject = new SwMapArea(room, data);
                    return true;
                case "trigger":
                    mapObject = new SwMapTrigger(room, data);
                    return true;
                case "checkpoint":
                    mapObject = new SwMapCheckpoint(room, data);
                    return true;
                case "spawner":
                    mapObject = new SwMapSpawner(room, data);
                    return true;
                default:
                    return ErEngine.LogWarning("invalid type for map object '", type, "'");
            }
        }
        catch(Exception e)
        {
            return ErEngine.LogWarning(e);
        }
    }
}