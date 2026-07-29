using ErisMath;
using Prion.Node;

namespace SpoonWitch.Game.Map;

public class SwMapObject
{
    public readonly SwRoom Room;
    public readonly string Id;
    public readonly string Type;
    public readonly ErRect2I Rect;
    public readonly PriDict Data;
    private SwMapObject(SwRoom room, string id, ErRect2I rect, string type, PriDict data)
    {
        Room = room;
        Id = id;
        Rect = rect;
        Type = type;
        Data = data;
    }
    public static bool TryFromData(SwRoom room, PriNode data, out SwMapObject mapObject)
    {
        mapObject = null!;
        if(!data.Get("iid").TryAs(out string id)) return false;
        if(!data.Get("__identifier").TryAs(out string type)) return false;        
        if(!data.Get("__worldX").TryAs(out int xPx)) return false;
        if(!data.Get("__worldY").TryAs(out int yPx)) return false;
        if(!data.Get("width").TryAs(out int widthPx)) return false;
        if(!data.Get("height").TryAs(out int heightPx)) return false;
        ErRect2I rect = new(xPx, yPx, widthPx, heightPx);
        if(!data.Get("fieldInstances").TryAs(out PriList fieldList)) return false;
        PriDict fields = new();
        foreach (var item in fieldList.Values)
        {
            if(!item.Get("__identifier").TryAs(out string key)) return false;
            var value = item.Get("__value");
            fields.Data.Add(key, value);
        }
        mapObject = new(room, id, rect, type, fields);
        return true;
    }
}