using Eris;
using ErisMath;

namespace SpoonWitch.Game.Map;

public class SwSector
{
    public readonly ErRect2I RectTiles;
    public readonly ErVec2I PositionSectors;
    private readonly int[][] Layers;
    public SwSector(ErVec2I positionSectors, ErVec2I sizeTiles, int numTileLayers)
    {
        PositionSectors = positionSectors;
        RectTiles = new(positionSectors * sizeTiles, sizeTiles);
        Layers = new int[numTileLayers][];
        for (int idx = 0; idx < numTileLayers; idx++)
        {
            Layers[idx] = [];
        }
    }
    private int GetTileIdx(ErVec2I tileCoord)
    {
        return tileCoord.Y * RectTiles.Size.X + tileCoord.X;
    }
    public void SetTile(int layer, ErVec2I tileCoord, int tileId)
    {
        if (!RectTiles.Contains(tileCoord))
        {
            ErEngine.LogWarning("attempted to set tile ", tileCoord, " of sector ", PositionSectors, ". coord out of bounds");
            return;
        }
        tileCoord -= RectTiles.Position;
        int idx = GetTileIdx(tileCoord);
        if(Layers[layer].Length == 0)
        {
            Layers[layer] = new int[RectTiles.Size.X * RectTiles.Size.Y];
            Array.Fill(Layers[layer], -1);
        }
        Layers[layer][idx] = tileId;
    }
    public int GetTile(int layerIdx, ErVec2I tileCoord)
    {
        if (!RectTiles.Contains(tileCoord))
        {
            ErEngine.LogWarning("attempted to set tile ", tileCoord, " of sector ", PositionSectors, ". coord out of bounds");
            return -1;
        }
        return GetTilePriv(layerIdx, tileCoord);
    }
    private int GetTilePriv(int layerIdx, ErVec2I tileCoord)
    {
        if(Layers[layerIdx].Length == 0) return -1;
        tileCoord -= RectTiles.Position;
        int idx = GetTileIdx(tileCoord);
        return Layers[layerIdx][idx];
    }
    public int GetTopTile(ErVec2I tileCoord)
    {
        if (!RectTiles.Contains(tileCoord))
        {
            ErEngine.LogWarning("attempted to set tile ", tileCoord, " of sector ", PositionSectors, ". coord out of bounds");
            return -1;
        }
        for (int layerIdx = Layers.Length - 1; layerIdx >= 0; layerIdx--)
        {
            int tileId = GetTilePriv(layerIdx, tileCoord);
            if(tileId >= 0) return tileId;
        }
        // for (int layerIdx = 0; layerIdx < Layers.Length; layerIdx++)
        // {
        //     int tileId = GetTilePriv(layerIdx, tileCoord);
        //     if(tileId >= 0) return tileId;
        // }
        return -1;
    }
    public IEnumerable<(int layerIdx, ErVec2I tileCoord, int tileId)> GetTiles()
    {
        for (int layerIdx = 0; layerIdx < Layers.Length; layerIdx++)
        {
            if(Layers[layerIdx].Length == 0) continue;
            for (int yi = 0; yi < RectTiles.Size.Y; yi++)
            {
                for (int xi = 0; xi < RectTiles.Size.X; xi++)
                {
                    int tileIdx = GetTileIdx(new(xi,yi));
                    int tileId = Layers[layerIdx][tileIdx];
                    if(tileId == -1) continue;
                    var tileCoord = new ErVec2I(xi,yi) + RectTiles.Position;
                    yield return (layerIdx, tileCoord, tileId);
                }
            }
        }
    }
}