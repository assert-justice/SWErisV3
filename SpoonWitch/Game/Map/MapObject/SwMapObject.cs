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
    // private readonly string Dirpath;
    public virtual bool IsGlobal => Data.TryGet("is_global", out bool isGlobal) && isGlobal;
    public SwMapObject(PriNode data)
    {
        Data = data;
        if(!data.Get("type").TryAs(out Type)) throw new("no type");
        // if(!data.TryGet("dirpath", out Dirpath)) throw new("no dirpath");
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
        var center = RectPx.Center;
        Fields.TrySet("x", center.X);
        Fields.TrySet("y", center.Y);
    }
    public virtual void Trigger(){}
    public virtual void Update(){}
    // public PriNode GetData()
    // {
    //     PriDict dict = new();
    //     dict.Data["id"] = new PriString(Id);
    //     dict.Data["type"] = new PriString(Type);
    //     dict.TrySet("dirpath")
    //     dict.Data["x_px"] = new PriNumber(RectPx.Position.X);
    //     dict.Data["y_px"] = new PriNumber(RectPx.Position.Y);
    //     dict.Data["width_px"] = new PriNumber(RectPx.Size.X);
    //     dict.Data["height_px"] = new PriNumber(RectPx.Size.Y);
    //     dict.Data["x_t"] = new PriNumber(RectTiles.Position.X);
    //     dict.Data["y_t"] = new PriNumber(RectTiles.Position.Y);
    //     dict.Data["width_t"] = new PriNumber(RectTiles.Size.X);
    //     dict.Data["height_t"] = new PriNumber(RectTiles.Size.Y);
    //     dict.Data["fields"] = Fields;
    //     return dict;
    // }
    public virtual void Load()
    {
        // ErEngine.Log("loaded ", Type, " with id ", Id);
    }
    protected PriNode GetProps()
    {
        return Fields.DeepCopy();
    }
    public virtual void Unload(){}
    public virtual void Draw(){}
    private static bool TryLdtkToInternal(ErVec2I tileSize, PriNode ldtkData, string dirpath, out PriNode data)
    {
        data = new PriDict();
        if(!ldtkData.Get("iid").TryAs(out string id)) throw new("no id");
        if(!ldtkData.Get("__identifier").TryAs(out string type)) throw new("no type");
        if(!ldtkData.Get("__worldX").TryAs(out int xPx)) throw new("no world x");
        if(!ldtkData.Get("__worldY").TryAs(out int yPx)) throw new("no world y");
        if(!ldtkData.Get("width").TryAs(out int widthPx)) throw new("no width");
        if(!ldtkData.Get("height").TryAs(out int heightPx)) throw new("no height");
        if(!ldtkData.Get("fieldInstances").TryAs(out PriList fieldList)) throw new("no fields");
        PriDict fields = [];
        foreach (var item in fieldList.Values)
        {
            if(!item.Get("__identifier").TryAs(out string fieldName)) throw new("no field name");
            PriNode value = item.Get("__value");
            if(!item.TryGet("__type", out string fieldType)) throw new("missing field type");
            if(fieldType == "String")
            {
                if(value is PriNull) continue;
                if(!value.TryAs(out string src)) throw new("property overrides field must be a string");
                if (fieldName.EndsWith("_json"))
                {
                    if(!SwApp.TryParseJsonToPrion(src, out value)) return ErEngine.LogWarning("failed to parse json field '", fieldName, "'");
                }
            }
            fields.Add(fieldName, value);
        }
        data.TrySet("dirpath", dirpath);
        data.TrySet("id", id);
        data.TrySet("type", type);
        data.TrySet("x_px", xPx);
        data.TrySet("y_px", yPx);
        data.TrySet("width_px", widthPx);
        data.TrySet("height_px", heightPx);
        data.TrySet("x_t", xPx/tileSize.X);
        data.TrySet("y_t", yPx/tileSize.Y);
        data.TrySet("width_t", widthPx/tileSize.X);
        data.TrySet("height_t", heightPx/tileSize.Y);
        data.TrySet("fields", fields);
        return true;
    }
    public static bool TryFromLdtkData(ErVec2I tileSize, PriNode ldtk, string dirpath, out SwMapObject mapObject)
    {
        mapObject = null!;
        try
        {
            if(!TryLdtkToInternal(tileSize, ldtk, dirpath, out var data)) ErEngine.LogWarning("failed to convert");
            if(!data.TryGet("type", out string type)) return ErEngine.LogWarning("bad object type");
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
                case "prop":
                    mapObject = new SwMapProp(data);
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