using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Map.MapData;

public readonly struct SwRoomData
{
    public string Iid{get; init;}
    public string Name{get; init;}
    public ErRect2I RectSectors{get; init;}
    public string[] ObjectIds{get; init;}
    public SwMapTileSpan[] TileSpans{get; init;}
    public string[] AdjRoomIds{get; init;}
    public PriDict Fields{get; init;}
    public PriNode ToPri()
    {
        PriDict res = [];
        res.TrySet("iid", Iid);
        if(Name != Iid) res.TrySet("name", Name);
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
        PriList adjRooms = [];
        res.Add("adj_room_ids", adjRooms);
        foreach (var item in AdjRoomIds)
        {
            adjRooms.Add(new PriString(item));
        }
        res.Add("fields", Fields);
        return res;
    }
    public static bool TryFromData(out SwRoomData roomData, PriNode data)
    {
        roomData = default;
        if(!data.TryGet("iid", out string iid)) return ErEngine.LogWarning("room data missing id");
        if(!data.TryGet("name", out string name)) name = iid;
        if(!SwPrion.TryGetRect2I(out var rectSectors, data.Get("rect_sectors"))) return false;
        if(!data.TryGet("object_ids", out PriList objectIds)) return false;
        if(!data.TryGet("tile_spans", out PriList tileSpans)) return false;
        if(!data.TryGet("adj_room_ids", out PriList adjRooms)) return false;
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
        string[] adjRoomIds = new string[adjRooms.Count];
        for (int idx = 0; idx < adjRoomIds.Length; idx++)
        {
            if(!adjRooms.Data[idx].TryAs(out string s)) return false;
            adjRoomIds[idx] = s;
        }
        if(!data.TryGet("fields", out PriDict fields)) fields = [];
        roomData = new()
        {
            Iid = iid,
            Name = name,
            RectSectors = rectSectors,
            ObjectIds = ids,
            TileSpans = spans,
            AdjRoomIds = adjRoomIds,
            Fields = fields,
        };
        return true;
    }
}
