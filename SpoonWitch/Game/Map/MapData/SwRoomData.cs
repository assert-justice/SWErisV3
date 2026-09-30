using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Map.MapData;

public readonly struct SwRoomData
{
    public string Iid{get; init;}
    public ErRect2I RectSectors{get; init;}
    public string[] ObjectIds{get; init;}
    public SwMapTileSpan[] TileSpans{get; init;}
    public PriNode ToPri()
    {
        PriDict res = [];
        res.TrySet("iid", Iid);
        SwPrion.TrySetRect2I(res, "rect_sectors", RectSectors);
        PriList objIds = [];
        res.Add("object_ids", objIds);
        foreach (var id in ObjectIds)
        {
            objIds.Add(new PriString(id));
        }
        PriList spans = [];
        res.Add("tile_spans", spans);
        foreach (var span in TileSpans)
        {
            spans.Add(span.ToPri());
        }
        return res;
    }
    public static bool TryFromData(out SwRoomData roomData, PriNode data)
    {
        roomData = default;
        if(!data.TryGet("iid", out string iid)) return ErEngine.LogWarning("room data missing id");
        if(!SwPrion.TryGetRect2I(out var rectSectors, data.Get("rect_sectors"))) return false;
        if(!data.TryGet("object_ids", out PriList objectIds)) return false;
        if(!data.TryGet("tile_spans", out PriList tileSpans)) return false;
        string[] ids = new string[objectIds.Count];
        for (int idx = 0; idx < ids.Length; idx++)
        {
            if(!objectIds.Data[idx].TryAs(out string id)) return false;
            ids[idx] = id;
        }
        SwMapTileSpan[] spans = new SwMapTileSpan[tileSpans.Count];
        for (int idx = 0; idx < spans.Length; idx++)
        {
            if(!SwMapTileSpan.TryFromData(out var span, tileSpans.Data[idx])) return false;
            spans[idx] = span;
        }
        roomData = new()
        {
            Iid = iid,
            RectSectors = rectSectors,
            ObjectIds = ids,
            TileSpans = spans,
        };
        return true;
    }
}
