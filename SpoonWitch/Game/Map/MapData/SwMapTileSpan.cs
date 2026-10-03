using ErisMath;
using Prion.Node;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Map.MapData;

public readonly struct SwMapTileSpan
{
    public int TileId{get; init;}
    public int LayerIdx{get; init;}
    public ErVec2I TileCoord{get; init;}
    public int Length{get; init;}
    public PriNode ToPri()
    {
        PriDict dict = [];
        dict.TrySet("layer_idx", LayerIdx); 
        SwPrion.TrySetVec2I(dict, TileCoord);
        dict.TrySet("tile_id", TileId); 
        dict.TrySet("length", Length);
        return dict;
    }
    private bool IsNext(int layerIdx, ErVec2I tileCoord, int tileId, int length)
    {
        if(layerIdx != LayerIdx) return false;
        if(tileId != TileId) return false;
        if(tileCoord.Y != TileCoord.Y) return false;
        if(tileCoord.X != TileCoord.X + length) return false;
        return true;
    }
    public static bool TryFromData(out SwMapTileSpan span, PriNode data)
    {
        span = default;
        if(!data.TryGet("layer_idx", out int layer_idx)) return false;
        if(!SwPrion.TryGetVec2I(out var tileCoord, data)) return false;
        if(!data.TryGet("tile_id", out int tile_id)) return false;
        if(!data.TryGet("length", out int length)) length = 1;
        span = new()
        {
            LayerIdx = layer_idx,
            TileCoord = tileCoord,
            TileId = tile_id,
            Length = length,
        };
        return true;
    }
    public static bool TryFromList(out SwMapTileSpan[] spans, PriNode list)
    {
        spans = default!;
        int count = list.Count;
        if(count == 0) return false;
        spans = new SwMapTileSpan[count];
        int idx = 0;
        foreach (var spanData in list.Values)
        {
            if(!TryFromData(out var span, spanData)) return false;
            spans[idx] = span;
            idx++;
        }
        return true;
    }
    public class Builder
    {
        private readonly List<SwMapTileSpan> Spans = [];
        private SwMapTileSpan? Pending = null;
        private int Length = 0;
        public void Add(int layerIdx, ErVec2I tileCoord, int tileId)
        {
            if(Pending is not null && Pending.Value.IsNext(layerIdx, tileCoord, tileId, Length))
            {
                Length++;
                return;
            }
            Close();
            Pending = new()
            {
                LayerIdx = layerIdx,
                TileCoord = tileCoord,
                TileId = tileId,
            };
            Length = 1;
        }
        public SwMapTileSpan[] Drain()
        {
            Close();
            SwMapTileSpan[] res = [..Spans];
            Spans.Clear();
            return res;
        }
        private void Close()
        {
            if(Pending is null) return;
            SwMapTileSpan span = new()
            {
                LayerIdx = Pending.Value.LayerIdx,
                TileCoord = Pending.Value.TileCoord,
                TileId = Pending.Value.TileId,
                Length = Length,
            };
            Spans.Add(span);
            Pending = null;
            Length = 0;
        }
    }
}
