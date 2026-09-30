using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Map.Foliage;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Map.MapData;

public readonly struct SwMapData
{
    public string Iid{get; init;}
    public SwTileData TileData{get; init;}
    public SwFoliageData FoliageData{get; init;}
    public ErVec2I SectorSizeTiles{get; init;}
    public string[] EntityLayerNames{get; init;}
    public int NumTileLayers{get; init;}
    public SwMapObjectData DefaultCheckpoint{get; init;}
    public SwRoomData[] Rooms{get; init;}
    public SwMapObjectData[] Objects{get; init;}
    public PriNode ToPri()
    {
        PriDict res = [];
        res.TrySet("iid", Iid);
        res.TrySet("tile_data", TileData.ToPri());
        res.TrySet("foliage_data", FoliageData.ToPri());
        SwPrion.TrySetVec2I(res, "sector_size_tiles", SectorSizeTiles);
        PriList entityLayerNames = [];
        foreach(var name in EntityLayerNames)
        {
            entityLayerNames.Add(new PriString(name));
        }
        res.Add("entity_layer_names", entityLayerNames);
        res.TrySet("num_tile_layers", NumTileLayers);
        PriList rooms = [];
        res.Add("rooms", rooms);
        foreach (var room in Rooms)
        {
            rooms.Add(room.ToPri());
        }
        PriList objects = [];
        res.Add("objects", objects);
        foreach (var obj in Objects)
        {
            objects.Add(obj.ToPri());
        }
        res.Add("default_checkpoint", DefaultCheckpoint.ToPri());
        return res;
    }
    public static bool TryFromData(out SwMapData mapData, PriNode data)
    {
        mapData = default;
        if(!data.TryGet("iid", out string iid)) return false;
        if(!SwTileData.TryFromData(out var tileData, data.Get("tile_data"))) return false;
        if(!SwFoliageData.TryFromData(out var foliageData, data.Get("foliage_data"))) return false;
        if(!SwPrion.TryGetVec2I(out var sectorSizeTiles, data.Get("sector_size_tiles"))) return false;
        if(!data.TryGet("entity_layer_names", out PriList entityLayerNames)) return false;
        string[] entLayerNames = new string[entityLayerNames.Count];
        for (int idx = 0; idx < entLayerNames.Length; idx++)
        {
            if(!entityLayerNames.Data[idx].TryAs(out string layerName)) return false;
            entLayerNames[idx] = layerName;
        }
        if(!data.TryGet("num_tile_layers", out int numTileLayers)) return false;
        if(!data.TryGet("rooms", out PriList roomList)) return false;
        SwRoomData[] rooms = new SwRoomData[roomList.Count];
        for (int idx = 0; idx < rooms.Length; idx++)
        {
            if(!SwRoomData.TryFromData(out var roomData, roomList.Data[idx])) return false;
            rooms[idx] = roomData;
        }
        if(!data.TryGet("objects", out PriList objectList)) return false;
        SwMapObjectData[] objects = new SwMapObjectData[objectList.Count];
        for (int idx = 0; idx < objects.Length; idx++)
        {
            if(!SwMapObjectData.TryFromData(out var mapObjectData, objectList.Data[idx])) return false;
            objects[idx] = mapObjectData;
        }
        if(!SwMapObjectData.TryFromData(out var defaultCheckpoint, data.Get("default_checkpoint"))) return false;
        mapData = new()
        {
            Iid = iid,
            TileData = tileData,
            FoliageData = foliageData,
            SectorSizeTiles = sectorSizeTiles,
            EntityLayerNames = entLayerNames,
            NumTileLayers = numTileLayers,
            Rooms = rooms,
            Objects = objects,
            DefaultCheckpoint = defaultCheckpoint,
        };
        return true;
    }
    public static bool TryConvertLdtkData(out PriNode mapDataPri, PriNode ldtkData)
    {
        mapDataPri = PriNull.Null;
        if(!SwTileData.TryFromData(out var tileData, ldtkData.Get("tile_data"))) return false;
        if(!SwFoliageData.TryFromData(out var foliageData, ldtkData.Get("foliage_data"))) return false;
        if(!ldtkData.TryGet("ldtk_data", out PriDict data)) return ErEngine.LogWarning("missing ldtk_data");
        if(!data.TryGet("iid", out string mapIid)) return ErEngine.LogWarning("map missing id");
        PriDict res = [];
        res.TrySet("iid", mapIid);
        res.TrySet("tile_data", tileData.ToPri());
        res.TrySet("foliage_data", foliageData.ToPri());
        ErVec2I sectorSizePx = SwPrion.GetVec2I(data, "worldGridWidth", "worldGridHeight", new(640, 320));
        SwPrion.TrySetVec2I(res, "sector_size_tiles", sectorSizePx / tileData.TileSize);
        if(!data.TryGet("defs", out PriDict defs)) return ErEngine.LogWarning("map missing defs");
        if(!defs.TryGet("layers", out PriList layers)) return ErEngine.LogWarning("map missing layers");
        int numTileLayers = 0;
        PriList entityLayerNames = [];
        res.Add("entity_layer_names", entityLayerNames);
        foreach (var layerData in layers.Values)
        {
            if(!layerData.TryGet("type", out string layerType)) return ErEngine.LogWarning("map layer missing type");
            else if(layerType == "Tiles") numTileLayers++;
            else if(layerType == "Entities") entityLayerNames.Add(layerData.Get("identifier"));
        }
        res.TrySet("num_tile_layers", numTileLayers);
        PriList rooms = [];
        res.Add("rooms", rooms);
        PriList objects = [];
        res.Add("objects", objects);
        if(!data.TryGet("levels", out PriList levels)) return ErEngine.LogWarning("map missing levels");
        SwMapTileSpan.Builder builder = new();
        foreach (var levelData in levels.Values)
        {
            PriDict roomData = [];
            rooms.Add(roomData);
            if(!levelData.TryGet("iid", out string levelId)) return ErEngine.LogWarning("map level missing id");
            roomData.TrySet("iid", levelId);
            PriDict fields = [];
            foreach (var item in levelData.Get("fieldInstances").Values)
            {
                if(!item.TryGet("__identifier", out string key)) continue;
                if(key == "display_name") roomData.Add("name", item.Get("__value"));
                else fields.Add(key, item.Get("__value"));
            }
            var levelRectPx = SwPrion.GetRect2I(levelData, "worldX", "worldY", "pxWid", "pxHei");
            var levelRectSectors = levelRectPx / sectorSizePx;
            SwPrion.TrySetRect2I(roomData, "rect_sectors", levelRectSectors);
            PriList roomObjectIds = [];
            roomData.Add("object_ids", roomObjectIds);
            PriList roomTileSpans = [];
            roomData.Add("tile_spans", roomTileSpans);
            PriList neighbors = [];
            roomData.Add("adj_room_ids", neighbors);
            if(fields.Count > 0) roomData.Add("fields", fields);
            foreach (var item in levelData.Get("__neighbours").Values)
            {
                neighbors.Add(item.Get("levelIid"));
            }
            int tileLayerIdx = 0;
            
            foreach (var layer in levelData.Get("layerInstances").Values)
            {
                if(!layer.TryGet("__identifier", out string layerName)) layerName = string.Empty;
                if(!layer.TryGet("__type", out string layerType)) return ErEngine.LogWarning("map room layer missing type");
                switch (layerType)
                {
                    case "Entities":
                        foreach (var entityData in layer.Get("entityInstances").Values)
                        {
                            if(!SwMapObjectData.TryConvertLdtkData(out var mapObjectDataPri, entityData, tileData.TileSize, layerName))
                            {
                                ErEngine.LogWarning("failed to parse map object"); 
                                continue;
                            }
                            if(mapObjectDataPri.TryGet("class", out string className) 
                                && className == "checkpoint"
                                && mapObjectDataPri.Get("fields").TryGet("default", out bool isDefault)
                                && isDefault)
                            {
                                if(res.TryGet("default_checkpoint", out PriDict _)) ErEngine.LogWarning("multiple default checkpoints");
                                else res.TrySet("default_checkpoint", mapObjectDataPri);
                            }
                            objects.Add(mapObjectDataPri);
                            roomObjectIds.Add(mapObjectDataPri.Get("iid"));
                        }
                        break;
                    case "Tiles":
                        // List<SwMapTileSpan> tileSpans = [];
                        if(!layer.TryGet("gridTiles", out PriList tiles)) {ErEngine.LogWarning("no tiles"); continue;}
                        foreach (var tile in tiles.Values)
                        {
                            var px = tile.Get("px");
                            px.TryGet(0, out int x);
                            px.TryGet(1, out int y);
                            tile.Get("src").TryGet(0, out int tileId);
                            tileId /= 32;
                            ErVec2I posPx = new ErVec2I(x,y) + levelRectPx.Position;
                            var tileCoord = posPx / tileData.TileSize;
                            builder.Add(tileLayerIdx, tileCoord, tileId);
                        }
                        tileLayerIdx++;
                        break;
                    default:
                        ErEngine.LogWarning("unsupported layer type: ", layerType);
                        continue;
                }
            }
            foreach (var span in builder.Drain())
            {
                roomTileSpans.Add(span.ToPri());
            }
        }
        if(!res.TryGet("default_checkpoint", out PriDict _)) return ErEngine.LogWarning("no default checkpoint");
        mapDataPri = res;
        return true;
    }
}
