using Eris;
using ErisMath;

namespace SpoonWitch.Game.Map;

public class SwDisplayLayer
{
    public readonly SwMap Map;
    private struct SwTileDisplay
    {
        public int TileId;
        public SwTileMask Mask;
        public readonly ushort Seed{get; init;}
    }
    private readonly Dictionary<ErVec2I,SwTileDisplay> AtlasGrid = [];
    private readonly Dictionary<ErVec2I,int> TileGrid = [];
    private const int DefaultTileId = -1;
    private readonly List<(ErVec2I,int)> NextTiles = [];
    private static readonly ErVec2I[] Neighbors = [ErVec2I.Zero, ErVec2I.Right, ErVec2I.Down, ErVec2I.One];
    public SwDisplayLayer(SwMap map)
    {
        Map = map;
    }
    public void SetTile(ErVec2I tileCoord, int tileId)
    {
        TileGrid[tileCoord] = tileId;
        NextTiles.Add((tileCoord,tileId));
    }
    public int GetTileId(ErVec2I tileCoord)
    {
        if(!TileGrid.TryGetValue(tileCoord, out var val)) return DefaultTileId;
        return val;
    }
    private bool IsTileMatch(ErVec2I tileCoord, int tileId)
    {
        if(!TileGrid.TryGetValue(tileCoord, out var val)) return tileId == DefaultTileId;
        return tileId == val;
    }
    private SwTileMask GetMask(ErVec2I displayCoord, int tileId)
    {
        bool br = IsTileMatch(displayCoord - Neighbors[0], tileId);
        bool bl = IsTileMatch(displayCoord - Neighbors[1], tileId);
        bool tr = IsTileMatch(displayCoord - Neighbors[2], tileId);
        bool tl = IsTileMatch(displayCoord - Neighbors[3], tileId);
        SwTileMask mask = SwTileMask.None;
        if(br) mask |= SwTileMask.BottomRight;
        if(bl) mask |= SwTileMask.BottomLeft;
        if(tr) mask |= SwTileMask.TopRight;
        if(tl) mask |= SwTileMask.TopLeft;
        return mask;
    }
    private void UpdateDisplayTile(ErVec2I displayCoord, int tileId)
    {
        var mask = GetMask(displayCoord, tileId);
        if(!AtlasGrid.TryGetValue(displayCoord, out var tile))
        {
            tile = new()
            {
                Seed = (ushort)displayCoord.GetHashCode(),
            };
        }
        tile.Mask = mask;
        tile.TileId = tileId;
        AtlasGrid[displayCoord] = tile;
    }
    private void HandlePending()
    {
        if(NextTiles.Count == 0) return;
        foreach (var (tileCoord,tileId) in NextTiles)
        {
            foreach (var nei in Neighbors)
            {
                UpdateDisplayTile(tileCoord + nei, tileId);
            }
        }
        NextTiles.Clear();
    }
    public void Draw()
    {
        HandlePending();
        var tileSize = (ErVec2)Map.TileSize;
        var half = tileSize / 2;
        foreach (var (tilePos, tile) in AtlasGrid)
        {
            var tileData = Map.GetTileData(tile.TileId);
            var pos = (ErVec2)tilePos * tileSize - half;
            if(!tileData.TryDraw(pos, tile.Mask, tile.Seed, ErEngine.CurrentTime)) continue;// ErEngine.LogError("bad tile, coord: ", tilePos, " tile id: ", tile.TileId, " mask: ", tile.Mask, " seed: ", tile.Seed);
        }
    }
}