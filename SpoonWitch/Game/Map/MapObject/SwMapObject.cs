using Eris;
using ErisMath;
using Prion.Node;

namespace SpoonWitch.Game.Map.MapObject;

public abstract class SwMapObject
{
    public readonly string Id;
    public readonly string Type;
    public readonly ErRect2I RectTiles;
    public readonly ErRect2 RectPx;
    public readonly PriNode Fields;
    public readonly PriNode Data;
    // public virtual bool IsGlobal => false;
    public virtual bool IsGlobal => Fields.TryGet("is_global", out bool isGlobal) && isGlobal;
    public SwMapObject(PriNode data)
    {
        Data = data;
        if(!data.Get("type").TryAs(out Type)) throw new("no type");
        if(!data.Get("id").TryAs(out Id)) throw new("no id");
        if(!data.Get("x_px").TryAs(out int xPx)) throw new("no world x");
        if(!data.Get("y_px").TryAs(out int yPx)) throw new("no world y");
        if(!data.Get("width_px").TryAs(out int widthPx)) throw new("no width");
        if(!data.Get("height_px").TryAs(out int heightPx)) throw new("no height");
        if(!data.Get("x_t").TryAs(out int xTiles)) throw new("no world x");
        if(!data.Get("y_t").TryAs(out int yTiles)) throw new("no world y");
        if(!data.Get("width_t").TryAs(out int widthTiles)) throw new("no width");
        if(!data.Get("height_t").TryAs(out int heightTiles)) throw new("no height");
        RectTiles = new(xTiles, yTiles, widthTiles, heightTiles);
        RectPx = new(xPx, yPx, widthPx, heightPx);
        if(!data.Get("fields").TryAs(out Fields)) throw new("no fields");
    }
    public virtual void Trigger(){}
    public virtual void Update(){}
    public PriNode GetData()
    {
        PriDict dict = new();
        dict.Data["id"] = new PriString(Id);
        dict.Data["type"] = new PriString(Type);
        dict.Data["x_px"] = new PriNumber(RectPx.Position.X);
        dict.Data["y_px"] = new PriNumber(RectPx.Position.Y);
        dict.Data["width_px"] = new PriNumber(RectPx.Size.X);
        dict.Data["height_px"] = new PriNumber(RectPx.Size.Y);
        dict.Data["x_t"] = new PriNumber(RectTiles.Position.X);
        dict.Data["y_t"] = new PriNumber(RectTiles.Position.Y);
        dict.Data["width_t"] = new PriNumber(RectTiles.Size.X);
        dict.Data["height_t"] = new PriNumber(RectTiles.Size.Y);
        dict.Data["fields"] = Fields;
        return dict;
    }
    public virtual void Load()
    {
        ErEngine.Log("loaded ", Type, " with id ", Id);
    }
    public virtual void Unload(){}
    private static bool TryLdtkToInternal(ErVec2I tileSize, PriNode ldtkData, out PriNode data)
    {
        data = new PriDict();
        if(!ldtkData.Get("iid").TryAs(out string id)) throw new("no id");
        if(!ldtkData.Get("__identifier").TryAs(out string type)) throw new("no type");
        if(!ldtkData.Get("__worldX").TryAs(out int xPx)) throw new("no world x");
        if(!ldtkData.Get("__worldY").TryAs(out int yPx)) throw new("no world y");
        if(!ldtkData.Get("width").TryAs(out int widthPx)) throw new("no width");
        if(!ldtkData.Get("height").TryAs(out int heightPx)) throw new("no height");
        if(!ldtkData.Get("fieldInstances").TryAs(out PriList fieldList)) throw new("no fields");
        PriDict fields = new();
        foreach (var item in fieldList.Values)
        {
            if(!item.Get("__identifier").TryAs(out string key)) throw new("no field name");
            var value = item.Get("__value");
            fields.TrySet(key, value);
        }
        data.TrySet("id", new PriString(id));
        data.TrySet("type", new PriString(type));
        data.TrySet("x_px", new PriNumber(xPx));
        data.TrySet("y_px", new PriNumber(yPx));
        data.TrySet("width_px", new PriNumber(widthPx));
        data.TrySet("height_px", new PriNumber(heightPx));
        data.TrySet("x_t", new PriNumber(xPx/tileSize.X));
        data.TrySet("y_t", new PriNumber(yPx/tileSize.Y));
        data.TrySet("width_t", new PriNumber(widthPx/tileSize.X));
        data.TrySet("height_t", new PriNumber(heightPx/tileSize.Y));
        data.TrySet("fields", fields);
        return true;
    }
    public static bool TryFromLdtkData(ErVec2I tileSize, PriNode ldtk, out SwMapObject mapObject)
    {
        mapObject = null!;
        try
        {
            if(!TryLdtkToInternal(tileSize, ldtk, out var data)) return false;
            if(!data.TryGet("type", out string type)) return false;
            switch (type)
            {
                case "area":
                    mapObject = new SwMapArea(data);
                    return true;
                case "trigger":
                    mapObject = new SwMapTrigger(data);
                    return true;
                case "checkpoint":
                    mapObject = new SwMapCheckpoint(data);
                    return true;
                case "spawner":
                    mapObject = new SwMapSpawner(data);
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