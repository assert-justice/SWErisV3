using Eris;
using ErisMath;

namespace SpoonWitch.Utils;

public readonly struct SwTileSplitter
{
    private static readonly Queue<ErRect2> TileQueue = [];
    public readonly ErVec2 Offset;
    public readonly ErVec2 Padding;
    public readonly ErVec2 HalfPad;
    public readonly ErVec2 FullSize;
    public readonly ErVec2 TileSize;
    public readonly ErVec2I GridSize;
    public readonly int NumFrames;
    public SwTileSplitter(ErVec2 textureSize, ErVec2 tileSize, ErVec2? tileOffset = null, ErVec2? tilePadding = null)
    {
        Offset = tileOffset ?? ErVec2.Zero;
        Padding = tilePadding ?? ErVec2.Zero;
        HalfPad = Padding * 0.5;
        FullSize = tileSize + Padding;
        TileSize = tileSize;
        GridSize = ((textureSize - Offset + Padding) / FullSize).FloorToInt();
        NumFrames = GridSize.GetArea();
    }
    public bool OnGrid(ErVec2I tileCoord)
    {
        if(tileCoord.X < 0 || tileCoord.X > GridSize.X) return false;
        if(tileCoord.Y < 0 || tileCoord.Y > GridSize.Y) return false;
        return true;
    }
    public bool OnGrid(int tileIdx)
    {
        return tileIdx >= 0 && tileIdx < NumFrames;
    }
    public ErVec2I ToTileCoord(int tileIdx)
    {
        return new(tileIdx % GridSize.X, tileIdx / GridSize.X);
    }
    public bool TryGetTile(out ErRect2 tileRect, int tileIdx)
    {
        return TryGetTile(out tileRect, ToTileCoord(tileIdx));
    }
    public bool TryGetTile(out ErRect2 tileRect, ErVec2I tileCoord)
    {
        tileRect = default;
        if(!OnGrid(tileCoord)) return false;
        ErVec2 pos = Offset + (ErVec2)tileCoord* FullSize - HalfPad;
        tileRect = new(pos, TileSize);
        return true;
    }
    public IEnumerable<ErRect2> GetAllTiles()
    {
        for (int idx = 0; idx < NumFrames; idx++)
        {
            if(TryGetTile(out var frame, idx)) yield return frame;
        }
    }
    private static IEnumerable<ErRect2> DrainTileQueue()
    {
        while(TileQueue.TryDequeue(out var tile)) yield return tile;
    }
    public bool TryGetTiles(out IEnumerable<ErRect2> tileRects, IEnumerable<ErVec2I> tileCoords)
    {
        tileRects = default!;
        TileQueue.Clear();
        foreach (var item in tileCoords)
        {
            if(!TryGetTile(out var tileRect, item))
            {
                ErEngine.LogWarning("bad tile: ", item, " idx ", GridSize.X * item.Y + item.X, " size ", GridSize);
                TileQueue.Clear();
                return false;
            }
            TileQueue.Enqueue(tileRect);
        }
        tileRects = DrainTileQueue();
        return true;
    }
    public bool TryGetTiles(out IEnumerable<ErRect2> tileRects, IEnumerable<int> tileIndices)
    {
        var grid = this;
        return TryGetTiles(out tileRects, [..tileIndices.Select(grid.ToTileCoord)]);
    }
}