using Eris;
using ErisMath;

namespace SpoonWitch.Game.Map;

public class SwSector
{
    private readonly List<Dictionary<ErVec2I,int>> Layers = [];
    public readonly SwMap Map;
    public readonly SwRoom Room;
    public readonly ErVec2I PositionTiles;
    public readonly ErVec2I PositionSectors;
    public int NumLayers{get => Layers.Count;}
    public bool IsDirty{get; private set;}
    public SwSector(SwMap map, SwRoom room, ErVec2I positionSectors)
    {
        Map = map;
        Room = room;
        PositionSectors = positionSectors;
        PositionTiles = positionSectors * map.SectorSizeTiles;
    }
    public bool TrySetTile(ErVec2I tilePosition, int layerIdx, int tileId)
    {
        // Todo: set dirty flag
        if(layerIdx < 0) return ErEngine.LogWarning("layer index cannot be negative");
        if(layerIdx > 255) return ErEngine.LogWarning("layer index too high");
        if(tilePosition / Map.SectorSizeTiles != PositionSectors) return ErEngine.LogWarning("tile position is out of bounds");
        while(Layers.Count <= layerIdx) Layers.Add([]);
        Layers[layerIdx][tilePosition] = tileId;
        return true;
    }
    public void Clean()
    {
        if(!IsDirty) return;
        IsDirty = false;
    }
    public void Draw(){}
}