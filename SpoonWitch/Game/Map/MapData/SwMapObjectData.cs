using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Data;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Map.MapData;

public class SwMapObjectData
{
    public string Iid{get; init;} = string.Empty;
    public string Type{get; init;} = string.Empty;
    public ErRect2I RectPx{get; init;}
    public ErRect2I RectTiles{get; init;}
    public PriDict Fields{get; init;} = [];
    public static bool TryFromLdtkData(out SwMapObjectData mapObjectData, ErVec2I tileSize, PriNode data)
    {
        mapObjectData = default!;
        if(!data.TryGet("iid", out string id)) return ErEngine.LogWarning("map object data missing id");
        var rectPx = SwPrion.GetRect2I(data, "__worldX", "__worldY", "width", "height");
        var rectTiles = rectPx / tileSize;
        PriDict fields = [];
        fields.Add("type", data.Get("__identifier"));
        SwPrion.TrySetRect2I(fields, "rect_px", rectPx);
        SwPrion.TrySetRect2I(fields, "rect_tiles", rectTiles);
        if(data.TryGet("fieldInstances", out PriList fieldEntries))
        {
            foreach (var item in fieldEntries.Values)
            {
                if(!item.TryGet("__identifier", out string key)) continue;
                switch (key)
                {
                    case "property_overrides_json":
                        if(!item.TryGet("__value", out string s)) continue;
                        if(!SwData.TryParseJsonToPrion(s, out var priNode)){ErEngine.LogWarning("failed to parse property overrides json"); continue;}
                        fields.Merge(priNode);
                    break;
                    // Note: this should be handled in the map object spawning code
                    // case "class":
                    //     if(!item.TryGet("__value", out s)) continue;
                    //     fields.TrySet("type", s);
                    // break;
                    default:
                        if (key.EndsWith("json"))
                        {
                            if(!item.TryGet("__value", out s)) continue;
                            if(!SwData.TryParseJsonToPrion(s, out priNode)){ErEngine.LogWarning("failed to parse property overrides json"); continue;}
                            fields.TrySet(s, priNode);
                        }
                        else fields.Data[key] = item.Get("__value");
                    break;
                }
            }
        }
        if(!fields.TryGet("type", out string objType)) throw new("should be unreachable");
        mapObjectData = new()
        {
            Iid = id,
            Type = objType,
            RectPx = rectPx,
            RectTiles = rectTiles,
            Fields = fields,
        };
        return mapObjectData is not null;
    }
}
