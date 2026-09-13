using Eris;
using ErisMath;
using Prion.Db;
using Prion.Node;
using SpoonWitch.Game.Map.MapObject;

namespace SpoonWitch.Game.Map;

public class SwRoom
{
    public readonly HashSet<ErVec2I> SectorCoords = [];
    private readonly SwMapObjectLookup MapObjects = new();
    public readonly SwMap Map;
    public readonly string Id;
    public readonly ErRect2I RectSectors;
    public readonly ErRect2I RectTiles;
    public readonly ErRect2 RectPx;
    public readonly PriDb Props;
    public readonly string DisplayName = string.Empty;
    public bool IsDirty{get; private set;}
    public readonly Dictionary<(int layerIdx, ErVec2I tileCoord), int> TileLookup = [];
    private SwRoom(SwMap map, PriNode data)
    {
        Map = map;
        if(!data.TryGet("iid", out Id)) throw new("missing field");
        if(!data.TryGet("worldX", out int xPx)) throw new("missing field");
        if(!data.TryGet("worldY", out int yPx)) throw new("missing field");
        if(!data.TryGet("pxWid", out int widthPx)) throw new("missing field");
        if(!data.TryGet("pxHei", out int heightPx)) throw new("missing field");
        if(!data.TryGet("layerInstances", out PriList layers)) throw new("missing field");
        if(!data.TryGet("fieldInstances", out PriList fields)) throw new("missing field");
        RectSectors = new ErRect2I(xPx, yPx, widthPx, heightPx) / map.SectorSizePx;
        foreach (var item in RectSectors.GetInnerCoords())
        {
            SectorCoords.Add(item);
        }
        RectTiles = RectSectors * map.SectorSizeTiles;
        RectPx = (ErRect2)(RectTiles * map.TileSize);
        PriDict props = [];
        foreach (var val in fields.Data)
        {
            if(!val.TryGet("__identifier", out string key)) throw new("missing field");
            var value = val.Get("__value");
            props.TrySet(key, value);
        }
        Props = new(props);
        int layerIdx = 0;
        foreach (var layer in layers.Values)
        {
            if(!layer.Get("__type").TryAs(out string layerType)) throw new("malformed layer");
            if(layerType == "Entities")
            {
                if(!TryAddEntityLayer(layer)) throw new("bad");
            }
            else if(layerType == "Tiles")
            {
                if(!TryAddTileLayer(layer, map.NumTileLayers - 1 - layerIdx)) throw new("bad");
                layerIdx++;
            }
            else throw new($"bad layer type '{layerType}'.");
        }
        if(props.TryGet("room_auto_mode", out string autoMode))
        {
            switch (autoMode)
            {
                case "walled_grassy":
                    AutoWalledGrassy();
                    break;
                default:
                    ErEngine.LogWarning("unknown auto mode ", autoMode);
                    break;
            }
        }
    }
    public void Update()
    {
        foreach (var item in MapObjects.GetObjects())
        {
            item.Update();
        }
    }
    private void AddMapObject(SwMapObject mapObject)
    {
        if(mapObject.IsGlobal) Map.AddGlobalObject(mapObject);
        else MapObjects.AddObject(mapObject);
    }
    private bool TryAddEntityLayer(PriNode layerData)
    {
        var entList = layerData.Get("entityInstances");
        if(entList is PriNull) return ErEngine.LogWarning("entity layer has no instances field");
        foreach (var entData in entList.Values)
        {
            if(!SwMapObject.TryFromLdtkData(Map.TileSize, entData, Map.Dirpath, out var mapObject))
            {
                ErEngine.LogWarning("malformed map object");
                continue;
            }
            if(mapObject.IsGlobal) Map.AddGlobalObject(mapObject);
            else AddMapObject(mapObject);
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
            ErVec2I tileCoord = RectTiles.Position + new ErVec2I(xPx, yPx) / Map.TileSize;
            TileLookup[(layerIdx, tileCoord)] = tileId;
        }
        return true;
    }
    private static IEnumerable<ErVec2I> GetEdgeCoords(ErRect2I rect)
    {
        // Note: this is split up like this for sector coherency
        for (int xi = rect.Left; xi < rect.Right; xi++)
        {
            yield return new(xi, rect.Top);
        }
        for (int xi = rect.Left; xi < rect.Right; xi++)
        {
            yield return new(xi, rect.Bottom - 1);
        }
        for (int yi = rect.Top; yi < rect.Bottom; yi++)
        {
            yield return new(rect.Left, yi);
        }
        for (int yi = rect.Top; yi < rect.Bottom; yi++)
        {
            yield return new(rect.Right - 1, yi);
        }
    }
    private void AutoWalledGrassy()
    {
        foreach (var coord in RectTiles.GetInnerCoords())
        {
            TileLookup.TryAdd((2, coord), 4);
        }
        foreach (var coord in GetEdgeCoords(RectTiles))
        {
            TileLookup.TryAdd((3, coord), 6);
        }
    }
    public void Clean()
    {
        if(!IsDirty) return;
        IsDirty = false;
    }
    public void LoadObjects()
    {
        foreach (var item in MapObjects.GetObjects())
        {
            item.Load();
        }
    }
    public void Unload(){}
    public void Draw()
    {
        foreach (var item in MapObjects.GetObjects())
        {
            item.Draw();
        }
    }
    public static bool TryFromData(SwMap map, PriNode data, out SwRoom room)
    {
        room = null!;
        try
        {
            room = new(map, data);
        }
        catch (Exception e)
        {
            ErEngine.LogWarning(e);
        }
        return room is not null;
    }
}