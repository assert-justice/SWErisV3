using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Data;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Map.MapData;

public readonly struct SwMapObjectData
{
    public string Iid{get; init;}
    public string Name{get; init;}
    public string LayerName{get; init;}
    public string Class{get; init;}
    public ErRect2I RectTiles{get; init;}
    public PriDict Fields{get; init;}
    public PriNode GetProps()
    {
        PriDict res = [];
        res.Merge(ToPri());
        var prototype = SwData.Prototypes.Get($"map_entities/{Class}");
        res.Merge(prototype);
        return res;
    }
    public PriNode ToPri()
    {
        PriDict res = [];
        res.TrySet("iid", Iid);
        res.TrySet("name", Name);
        res.TrySet("layer_name", LayerName);
        res.TrySet("class", Class);
        SwPrion.TrySetRect2I(res, "rect_tiles", RectTiles);
        if(Fields.Count > 0) res.Add("fields", Fields);
        return res;
    }
    public static bool TryFromData(out SwMapObjectData mapObjectData, PriNode data)
    {
        mapObjectData = default;
        if(!data.TryGet("iid", out string iid)) return false;
        if(!data.TryGet("name", out string name)) return false;
        if(!data.TryGet("layer_name", out string layer_name)) return false;
        if(!data.TryGet("class", out string class_name)) return false;
        if(!SwPrion.TryGetRect2I(out var rect_tiles, data.Get("rect_tiles"))) return false;
        if(!data.TryGet("fields", out PriDict fields)) fields = [];
        mapObjectData = new()
        {
            Iid = iid,
            Name = name,
            LayerName = layer_name,
            Class = class_name,
            RectTiles = rect_tiles,
            Fields = fields,
        };
        return true;
    }
    public static bool TryConvertLdtkData(out PriNode mapObjectDataPri, PriNode ldtkData, ErVec2I tileSize, string layerName)
    {
        mapObjectDataPri = PriNull.Null;
        if(!ldtkData.TryGet("iid", out string iid)) return ErEngine.LogWarning("map object data missing id");
        PriDict res = [];
        res.TrySet("iid", iid);
        if(!ldtkData.TryGet("__identifier", out string name)) return false;
        res.TrySet("name", name);
        string className = name;
        res.TrySet("layer_name", layerName);
        var rectPx = SwPrion.GetRect2I(ldtkData, "__worldX", "__worldY", "width", "height");
        var rectTiles = rectPx / tileSize;
        SwPrion.TrySetRect2I(res, "rect_tiles", rectTiles);
        PriDict fields = [];
        res.Add("fields", fields);
        PriNode propertyOverrides = PriNull.Null;
        if(ldtkData.TryGet("fieldInstances", out PriList fieldEntries))
        {
            foreach (var item in fieldEntries.Values)
            {
                if(!item.TryGet("__identifier", out string key)) continue;
                switch (key)
                {
                    case "property_overrides_json":
                        if(!item.TryGet("__value", out string s)) continue;
                        if(!SwData.TryParseJsonToPrion(s, out propertyOverrides)){ErEngine.LogWarning("failed to parse property overrides json"); continue;}
                        break;
                    case "class":
                        if(!item.TryGet("__value", out s)) continue;
                        className = s;
                    break;
                    default:
                        if (key.EndsWith("json"))
                        {
                            if(!item.TryGet("__value", out s)) continue;
                            if(!SwData.TryParseJsonToPrion(s, out var priNode)){ErEngine.LogWarning("failed to parse property overrides json"); continue;}
                            fields.TrySet(key, priNode);
                        }
                        else fields.Data[key] = item.Get("__value");
                    break;
                }
            }
        }
        res.TrySet("class", className);
        if(propertyOverrides.TryAs(out PriDict overrides)) fields.Merge(overrides);
        mapObjectDataPri = res;
        return true;
    }
}
