using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Map.MapData;

public class SwMapData
{
    public string Id{get; init;} = string.Empty;
    public SwTileData[] TileData{get; init;} = [];
    public ErVec2I TileSize{get; private set;}
    public ErVec2I SectorSizeTiles{get; private set;}
    public ErVec2I SectorSizePx{get; private set;}
    public int NumTileLayers{get; private set;}
    public readonly Dictionary<ErVec2I, SwSectorData> Sectors = [];
    public readonly Dictionary<string, SwRoomData> Rooms = [];
    public readonly Dictionary<string, SwMapObjectData> Objects = [];
    public readonly HashSet<string> GlobalObjectIds = [];
    private SwMapData(){}
    // private bool TryAddObjectLayerLdtk(SwRoomData roomData, PriNode layerData)
    // {
    //     return true;
    // }
    private bool TryAddTileLayerLdtk(int layerIdx, SwRoomData roomData, PriNode layerData)
    {
        if(!layerData.TryGet("gridTiles", out PriList tiles)) return false;
        foreach (var tile in tiles.Values)
        {
            var px = tile.Get("px");
            px.TryGet(0, out int x);
            px.TryGet(1, out int y);
            tile.Get("src").TryGet(0, out int tileIdx);
            tileIdx /= 32;
            ErVec2I posPx = new ErVec2I(x,y) + roomData.RectPx.Position;
            var tileCoord = posPx / TileSize;
            var sectorCoord = posPx / SectorSizePx;
            if(!Sectors.TryGetValue(sectorCoord, out var sectorData)) return ErEngine.LogWarning("bad sector coord: ", sectorCoord);
            sectorData.SetTile(layerIdx, tileCoord, tileIdx);
        }
        return true;
    }
    public static bool TryFromLdtkData(out SwMapData mapData, SwTileData[] tileData, PriNode data)
    {
        mapData = default!;
        if(!data.TryGet("iid", out string id)) return ErEngine.LogWarning("map missing id");
        mapData = new()
        {
            Id = id,
            TileData = tileData,
            TileSize = data.TryGet("defaultGridSize", out int tileWidth) ? new(tileWidth, tileWidth) : new(32,32),
            SectorSizePx = SwPrion.GetVec2I(data, "worldGridWidth", "worldGridHeight", new(640, 320)),
        };
        mapData.SectorSizeTiles = mapData.SectorSizePx / mapData.TileSize;
        if(!data.TryGet("defs", out PriDict defs)) return ErEngine.LogWarning("map missing defs");
        if(!defs.TryGet("layers", out PriList layers)) return ErEngine.LogWarning("map missing layers");
        int numTileLayers = 0;
        foreach (var layerData in layers.Values)
        {
            if(!layerData.TryGet("type", out string layerType)) return ErEngine.LogWarning("map layer missing type");
            else if(layerType == "Tiles") numTileLayers++;
        }
        mapData.NumTileLayers = numTileLayers;
        // parse levels/rooms
        if(!data.TryGet("levels", out PriList rooms)) return ErEngine.LogWarning("map missing levels");
        foreach (var roomDataLdtk in rooms.Values)
        {
            if(!roomDataLdtk.TryGet("iid", out string roomId)) return ErEngine.LogWarning("map level missing id");
            var rectPx = SwPrion.GetRect2I(roomDataLdtk, "worldX", "worldY", "pxWid", "pxHei");
            SwRoomData roomData = new()
            {
                Id = roomId,
                RectPx = rectPx,
                RectTiles = rectPx / mapData.TileSize,
                RectSectors = rectPx / mapData.SectorSizePx,
            };
            if(!mapData.Rooms.TryAdd(roomData.Id, roomData)) return ErEngine.LogWarning("duplicate room id: ", roomData.Id);
            foreach (var sectorCoord in roomData.RectSectors.GetInnerCoords())
            {
                SwSectorData sectorData = new(sectorCoord, mapData.SectorSizeTiles, mapData.NumTileLayers);
                if(!mapData.Sectors.TryAdd(sectorCoord, sectorData)) return ErEngine.LogWarning("duplicate sector coord : ", sectorCoord);
            }
            // Dictionary<ErVec2I, List<(ErVec2I tileCoord, int layerIdx, )>>
            int tileLayerIdx = 0;
            foreach (var layer in roomDataLdtk.Get("layerInstances").Values)
            {
                if(!layer.TryGet("__type", out string layerType)) return ErEngine.LogWarning("map room layer missing type");
                switch (layerType)
                {
                    case "Entities":
                        // mapData.TryAddObjectLayerLdtk(roomData, layer);
                        break;
                    case "Tiles":
                        if(!mapData.TryAddTileLayerLdtk(tileLayerIdx, roomData, layer)) return ErEngine.LogWarning("bad tile layer");
                        tileLayerIdx++;
                        break;
                    default:
                        // ErEngine.LogWarning("map layer unsupported type: ", layerType);
                        break;
                };
            }
        }
        return mapData is not null;
    }
}
