using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Map.MapObject;

namespace SpoonWitch.Game.Map;

public class SwRoom
{
    private readonly HashSet<ErVec2I> Sectors = [];
    private readonly Dictionary<string,SwMapObject> MapObjects = [];
    // private readonly Dictionary<string,Dictionary<string,SwMapObject>> MapObjectLookup = [];
    public readonly SwMap Map;
    public readonly string Id;
    public readonly ErRect2I RectSectors;
    public readonly ErRect2I RectTiles;
    public readonly ErRect2 RectPx;
    public bool IsDirty{get; private set;}

    private SwRoom(SwMap map, string id, ErRect2I rectSectors)
    {
        Map = map;
        Id = id;
        RectSectors = rectSectors;
        RectTiles = rectSectors * map.SectorSizeTiles;
        RectPx = (ErRect2)(RectTiles * map.TileSize);
    }
    public IEnumerable<ErVec2I> GetSectors()
    {
        foreach (var item in Sectors)
        {
            yield return item;
        }
    }
    public void Update()
    {
        foreach (var item in MapObjects.Values)
        {
            item.Update();
        }
    }
    private void AddMapObject(SwMapObject mapObject)
    {
        if(mapObject.IsGlobal) Map.AddGlobalObject(mapObject);
        else MapObjects.Add(mapObject.Id, mapObject);
        // if(!MapObjectLookup.TryGetValue(mapObject.Type, out var dict))
        // {
        //     dict = [];
        //     MapObjectLookup[mapObject.Type] = dict;
        // }
        // dict.Add(mapObject.Id, mapObject);
    }
    private bool TryAddEntityLayer(PriNode layerData)
    {
        if(!layerData.Get("entityInstances").TryAs(out PriList entList)) return false;
        foreach (var entData in entList.Values)
        {
            if(!SwMapObject.TryFromData(this, entData, out var mapObject)) return ErEngine.LogWarning("malformed map object");
            // ErEngine.Log(mapObject.Type);
            AddMapObject(mapObject);
        }
        return true;
    }
    private bool TryAddTileLayer(PriNode layerData, int layerIdx)
    {
        if(!layerData.Get("gridTiles").TryAs(out PriList tiles)) return false;
        foreach (var tileData in tiles.Values)
        {
            if(!tileData.Get("px").Get(0).TryAs(out int xPx)) return false;
            if(!tileData.Get("px").Get(1).TryAs(out int yPx)) return false;
            if(!tileData.Get("src").Get(0).TryAs(out int srcX)) return false;
            int tileId = ErMath.FloorToInt(srcX / 32);
            ErVec2I tilePos = RectTiles.Position + new ErVec2I(xPx, yPx) / Map.TileSize;
            ErVec2I sectorPos = tilePos / Map.SectorSizeTiles;
            Sectors.Add(sectorPos);
            // Todo: defer this
            Map.SetTile(layerIdx, tilePos, tileId);
            // if(!Sectors.TryGetValue(sectorPos, out var sector))
            // {
            //     sector = new(Map, this, sectorPos);
            //     Sectors.Add(sectorPos, sector);
            // }
            // if(!sector.TrySetTile(tilePos, layerIdx, tileId)) return false;
        }
        return true;
    }
    public void Clean()
    {
        if(!IsDirty) return;
        IsDirty = false;
    }
    public void Load(){}
    public void Unload(){}
    public void Draw(){}
    public static bool TryFromData(SwMap map, PriNode data, out SwRoom room)
    {
        room = null!;
        if(!data.Get("iid").TryAs(out string id)) return false;
        if(!data.Get("worldX").TryAs(out int xPx)) return false;
        if(!data.Get("worldY").TryAs(out int yPx)) return false;
        if(!data.Get("pxWid").TryAs(out int widthPx)) return false;
        if(!data.Get("pxHei").TryAs(out int heightPx)) return false;
        if(!data.Get("layerInstances").TryAs(out PriList layers)) return false;
        room = new(map, id, new ErRect2I(xPx, yPx, widthPx, heightPx) / map.SectorSizePx);
        int layerIdx = 0;
        foreach (var layer in layers.Values)
        {
            if(!layer.Get("__type").TryAs(out string layerType)) return ErEngine.LogWarning("malformed layer");
            if(layerType == "Entities")
            {
                if(!room.TryAddEntityLayer(layer)) return false;
            }
            else if(layerType == "Tiles")
            {
                if(!room.TryAddTileLayer(layer, layerIdx)) return false;
                layerIdx++;
            }
            else return ErEngine.LogWarning("bad layer type '", layerType, "'.");
        }
        return true;
    }
}