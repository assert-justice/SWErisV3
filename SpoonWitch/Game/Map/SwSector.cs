using Eris;
using ErisMath;

namespace SpoonWitch.Game.Map;

public class SwSector
{
    // private readonly List<Dictionary<ErVec2I,int>> Layers = [];
    // public readonly SwMap Map;
    // public readonly SwRoom Room;
    public readonly ErRect2I RectTiles;
    public readonly ErVec2I PositionSectors;
    private readonly int[][] Layers;
    public SwSector(SwMap map, ErVec2I positionSectors)
    {
        PositionSectors = positionSectors;
        RectTiles = new(positionSectors * map.SectorSizeTiles, map.SectorSizeTiles);
        Layers = new int[map.NumTileLayers][];
        for (int idx = 0; idx < map.NumTileLayers; idx++)
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
    public int GetTile(int layer, ErVec2I tileCoord)
    {
        if (!RectTiles.Contains(tileCoord))
        {
            ErEngine.LogWarning("attempted to set tile ", tileCoord, " of sector ", PositionSectors, ". coord out of bounds");
            return 0;
        }
        if(Layers[layer].Length == 0) return 0;
        int idx = GetTileIdx(tileCoord);
        return Layers[layer][idx];
    }
    public void Load(SwMap map)
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
                    map.SetTile(layerIdx, tileCoord, tileId);
                }
            }
        }
    }
    // public int NumLayers{get => Layers.Count;}
    // public bool IsDirty{get; private set;}
    // public SwSector(SwMap map, SwRoom room, ErVec2I positionSectors)
    // {
    //     Map = map;
    //     Room = room;
    //     PositionSectors = positionSectors;
    //     PositionTiles = positionSectors * map.SectorSizeTiles;
    // }
    // public bool TrySetTile(ErVec2I tilePosition, int layerIdx, int tileId)
    // {
    //     // Todo: set dirty flag
    //     if(layerIdx < 0) return ErEngine.LogWarning("layer index cannot be negative");
    //     if(layerIdx > 255) return ErEngine.LogWarning("layer index too high");
    //     if(tilePosition / Map.SectorSizeTiles != PositionSectors) return ErEngine.LogWarning("tile position is out of bounds");
    //     while(Layers.Count <= layerIdx) Layers.Add([]);
    //     Layers[layerIdx][tilePosition] = tileId;
    //     return true;
    // }
    // public void Clean()
    // {
    //     if(!IsDirty) return;
    //     IsDirty = false;
    // }
    // public void Draw(){}
}