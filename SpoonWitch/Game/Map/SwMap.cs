using System.Collections;
using Eris;
using Eris.Utils.Grid2D;
using ErisMath;
using ErisPhysics2D;
using SpoonWitch.Game.Entity.MapEntity;
using SpoonWitch.Game.Map.Foliage;
using SpoonWitch.Game.Map.MapData;
using SpoonWitch.Game.Map.MapDisplay;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Map;

public class SwMap
{
    private readonly SwMapDisplay MapDisplay;
    private readonly SwFoliage Foliage;
    private readonly SwGame Game;
    private readonly SwMapData MapData;
    private readonly Dictionary<string, int> RoomIdLookup = [];
    private readonly Dictionary<ErVec2I, string> RoomSectorLookup = [];
    private readonly Dictionary<string, SwRoom> LoadedRooms = [];
    private readonly Dictionary<string, int> MapObjectIdLookup = [];
    private readonly Dictionary<string, (int mapObjectIdx, int entityId)> LoadedMapObjects = [];
    private readonly ErHashGrid2D<SwSector> SectorGrid;
    private readonly Queue<Action> PostprocessQueue = [];
    public Action<SwRoom?,bool> OnNewCurrentRoom{get; set;}
    public SwTileData.Entry[] TileData => MapData.TileData.Entries;
    public SwFoliageData FoliageData => MapData.FoliageData;
    public ErVec2I TileSize => MapData.TileData.TileSize;
    public SwMapObjectData CurrentCheckpoint{get; private set;}
    public ErVec2 CurrentCheckpointPos => (ErVec2)(CurrentCheckpoint.RectTiles * TileSize).Center;
    public ErVec2I SectorSizeTiles => MapData.SectorSizeTiles;
    public ErVec2I SectorSizePx => MapData.SectorSizeTiles * TileSize;
    public SwRoom? CurrentRoom{get; private set;}
    public SwMap(SwGame game, SwMapData mapData)
    {
        OnNewCurrentRoom = (_,_)=>{};
        Game = game;
        MapData = mapData;
        SectorGrid = new();
        MapDisplay = new(this, mapData.NumTileLayers);
        Foliage = new(this);
        CurrentCheckpoint = mapData.DefaultCheckpoint;
        for (int idx = 0; idx < mapData.Rooms.Length; idx++)
        {
            string id = mapData.Rooms[idx].Iid;
            RoomIdLookup[id] = idx;
            foreach (var sectorCoord in mapData.Rooms[idx].RectSectors.GetInnerCoords())
            {
                RoomSectorLookup[sectorCoord] = id;
            }
        }
        for (int idx = 0; idx < mapData.Objects.Length; idx++)
        {
            MapObjectIdLookup[mapData.Objects[idx].Iid] = idx;
        }
    }
    public void Update(ErVec2 targetPoint)
    {
        var sectorCoord = (ErVec2I)targetPoint / SectorSizePx;
        HandleLoading(sectorCoord);
        // handle commands
    }
    public void Draw()
    {
        MapDisplay.Draw();
        Foliage.Draw();
    }
    public int GetTileId(int layerIdx, ErVec2I tileCoord)
    {
        var sectorCoord = tileCoord / MapData.SectorSizeTiles;
        if(!SectorGrid.TryGet(sectorCoord, out var sector)) return -2;
        return sector.GetTile(layerIdx, tileCoord);
    }
    public int GetTopTileId(ErVec2I tileCoord)
    {
        var sectorCoord = tileCoord / MapData.SectorSizeTiles;
        if(!SectorGrid.TryGet(sectorCoord, out var sector)) return -2;
        return sector.GetTopTile(tileCoord);
    }
    public bool InSameRoom(ErVec2 posA, ErVec2 posB)
    {
        var aSectorCoord = (ErVec2I)posA / SectorSizePx;
        if(!RoomSectorLookup.TryGetValue(aSectorCoord, out string? aRoomId)) return false;
        var bSectorCoord = (ErVec2I)posB / SectorSizePx;
        if(!RoomSectorLookup.TryGetValue(bSectorCoord, out string? bRoomId)) return false;
        return aRoomId == bRoomId;
    }
    private void HandleLoading(ErVec2I sectorCoord)
    {
        if(CurrentRoom is not null && CurrentRoom.RectSectors.Contains(sectorCoord)) return; // no work to do
        bool wasNull = CurrentRoom is null;
        if(!RoomSectorLookup.TryGetValue(sectorCoord, out string? roomId)) CurrentRoom = null;
        else CurrentRoom = LoadRoomData(roomId);
        if(CurrentRoom is null && wasNull) return; // no work to do
        OnNewCurrentRoom(CurrentRoom, wasNull);
        if(CurrentRoom is null) return; // the target position is out of bounds somehow, don't load or unload anything
        // get a hash set of the currently loaded room ids
        HashSet<string> loadedIds = [..LoadedRooms.Keys];
        loadedIds.Remove(CurrentRoom.Data.Iid);
        // for the room and all of its neighbors
        foreach (var adj in CurrentRoom.AdjRoomIds)
        {
            // if they are present in the hash set, that means they are loaded. remove them
            if(!loadedIds.Remove(adj)) LoadRoomData(adj);
        }
        // what remains in the hash set are the rooms that need to be freed. free them
        foreach (var adjId in loadedIds)
        {
            UnloadRoom(adjId);
        }
        // handle postprocessing
        while(PostprocessQueue.TryDequeue(out var action)) action();
    }
    private SwRoom? LoadRoomData(string roomId)
    {
        if(LoadedRooms.TryGetValue(roomId, out var room)) return room;
        if(!RoomIdLookup.TryGetValue(roomId, out int roomIdx))
        {
            ErEngine.LogWarning("invalid room id: ", roomId);
        }
        room = new(this, MapData.Rooms[roomIdx]);
        LoadedRooms[room.Data.Iid] = room;
        // create the sectors
        foreach (var sectorCoord in room.RectSectors.GetInnerCoords())
        {
            SetTile(0, sectorCoord * SectorSizeTiles, -1);
        }
        // set tiles
        foreach (var span in room.Data.TileSpans)
        {
            for (int idx = 0; idx < span.Length; idx++)
            {
                ErVec2I offset = new(idx, 0);
                SetTile(span.LayerIdx, span.TileCoord + offset, span.TileId);
            }
        }
        foreach (var tileCoord in room.RectTiles.GetInnerCoords())
        {
            int tileId = GetTopTileId(tileCoord);
            uint mask = uint.MaxValue;
            if(tileId == -1) mask = 0;
            else if(tileId >= 0) mask = TileData[tileId].CollisionMask;
            Game.PhysicsWorld.SetTileMask(tileCoord, mask);
        }
        // load map objects
        foreach (var mapObjectId in room.Data.ObjectIds)
        {
            LoadMapObject(mapObjectId);
        }
        // enqueue postprocessing
        PostprocessQueue.Enqueue(()=>HandleFoliage(room.RectTiles));
        // ErEngine.Log("loaded room: ", room.Data.Name);
        return room;
    }
    private void HandleFoliage(ErRect2I tileRect)
    {
        Foliage.SeedArea(tileRect);
        Foliage.LifeSimArea(tileRect, 1);
        Foliage.TrimArea(tileRect);
    }
    private void UnloadRoom(string roomId)
    {
        if(!LoadedRooms.TryGetValue(roomId, out var room))
        {
            ErEngine.LogWarning("failed to unload room, bad id: ", roomId);
            return;
        }
        LoadedRooms.Remove(roomId);
        // ErEngine.Log("unloaded room ", room.Data.Name);
        // remove sectors
        foreach (var sectorCoord in room.RectSectors.GetInnerCoords())
        {
            if(!SectorGrid.Remove(sectorCoord)) continue;
        }
        foreach (var tileCoord in room.RectTiles.GetInnerCoords())
        {
            MapDisplay.QueueTileUpdate(tileCoord);
            Foliage.QueueTileUpdate(tileCoord);
            // Todo: revisit this
            Game.PhysicsWorld.SetTileMask(tileCoord, uint.MaxValue);
        }
        foreach (var mapObjectId in room.Data.ObjectIds)
        {
            UnloadMapObject(mapObjectId);
        }
    }
    private void LoadMapObject(string mapObjectId)
    {
        if (LoadedMapObjects.ContainsKey(mapObjectId))
        {
            ErEngine.LogWarning("attempted to load already loaded map object: ", mapObjectId);
            return;
        }
        if(!MapObjectIdLookup.TryGetValue(mapObjectId, out int mapObjectIdx))
        {
            ErEngine.LogWarning("no map object with id ", mapObjectId, " exists");
            return;
        }
        // ErEngine.Log("loaded map entity ", MapData.Objects[mapObjectIdx].Name);
        var props = MapData.Objects[mapObjectIdx].GetProps();
        var rectPx = (ErRect2)(MapData.Objects[mapObjectIdx].RectTiles * TileSize);
        SwPrion.TrySetRect2(props, "rect_px", rectPx);
        SwPrion.TrySetVec2(props, rectPx.Center);
        // ErEngine.Log(props);
        string className = MapData.Objects[mapObjectIdx].Class;
        int entId = -1;// int.MaxValue;
        switch (className)
        {
            case "area":
                // areas do not have a map entity, so their id is -1
                entId = -1;
                break;
            case "prop":
                break;
            case "checkpoint":
                break;
            case "spawner":
                break;
            case "pickup":
                break;
            case "trigger":
                entId = Game.AddEntity<SwTrigger>(props).Id;
                break;
            default:
                ErEngine.LogWarning("unsupported map object class ", className);
                return;
        }
        LoadedMapObjects.Add(mapObjectId, (mapObjectIdx, entId));
    }
    private void UnloadMapObject(string mapObjectId)
    {
        if(!LoadedMapObjects.TryGetValue(mapObjectId, out var value))
        {
            ErEngine.LogWarning("failed to unload map object, bad id: ", mapObjectId);
            return;
        }
        LoadedMapObjects.Remove(mapObjectId);
        // get entity and tell it to unload
        if(value.entityId == -1){}
        else if(!Game.EntityLookup.TryGet(value.entityId.ToString(), out SwMapEntity mapEntity))
        {
            ErEngine.LogWarning("failed to unload map entity, no such entity for id: ", mapObjectId);
            return;
        }
        else mapEntity.Unload();
        // ErEngine.Log("unloaded map entity ", MapData.Objects[value.mapObjectIdx].Name);
    }
    private void SetTile(int layerIdx, ErVec2I tileCoord, int tileId)
    {
        var sectorCoord = tileCoord / SectorSizeTiles;
        SectorGrid.Get(sectorCoord, NewSector).SetTile(layerIdx, tileCoord, tileId);
        MapDisplay.QueueTileUpdate(tileCoord);
    }
    private SwSector NewSector(ErVec2I sectorCoord)
    {
        return new(sectorCoord, SectorSizeTiles, MapData.NumTileLayers);
    }
}

// using Eris;
// using Eris.Renderer;
// using ErisMath;
// using ErisPhysics2D;
// using Prion.Node;
// using SpoonWitch.Command;
// using SpoonWitch.Game.Map.Foliage;
// using SpoonWitch.Game.Map.MapObject;
// using SpoonWitch.Utils;

// namespace SpoonWitch.Game.Map;

// public class SwMap
// {
//     private readonly Dictionary<string,SwRoom> Rooms = [];
//     private readonly Dictionary<string,SwRoom> LoadedRooms = [];
//     private readonly Dictionary<ErVec2I, SwSector> SectorLookup = [];
//     private readonly Dictionary<ErVec2I,SwRoom> RoomLookup = [];
//     // private readonly SwTileData[] TileData;
//     private readonly SwDisplayLayer[] DisplayLayers;
//     public readonly int NumTileLayers;
//     public readonly ErPhysicsWorld2D PhysicsWorld;
//     public readonly SwFoliage Foliage;
//     public readonly string Id;
//     public readonly ErVec2I TileSize;
//     public readonly ErVec2I SectorSizeTiles;
//     public readonly ErVec2I SectorSizePx;
//     private readonly SwMapObjectLookup GlobalMapObjects = new();
//     private readonly SwCommandHandler CommandHandler = new(SwApp.CommandStore);
//     public readonly string Dirpath;
//     private SwSector? LastSector;
//     public SwMap(string dirpath = "", string id = "", int numTileLayers = 0, ErVec2I? tileSize = null, ErVec2I? sectorSizePx = null, SwTileData[]? tileData = null)
//     {
//         Dirpath = dirpath;
//         Id = id;
//         NumTileLayers = numTileLayers;
//         DisplayLayers = new SwDisplayLayer[numTileLayers];
//         for (int i = 0; i < DisplayLayers.Length; i++)
//         {
//             DisplayLayers[i] = new(this, i);
//         }
//         TileSize = tileSize ?? new(32, 32);
//         SectorSizePx = sectorSizePx ?? new(640, 320);
//         SectorSizeTiles = SectorSizePx / TileSize;
//         var TileData = tileData ?? [];
//         uint[] tileMaskLookup = [..TileData.Select(t => t.CollisionMask)];
//         static void debugDrawRect(ErRect2 rect, bool overlap, uint mask)
//         {
//             if(mask == 0) return;
//             ErEngine.Renderer.DebugDrawRect(overlap ? ErColor.Red : ErColor.Blue, rect, false);
//         }
//         static void debugDrawLine(ErVec2 start, ErVec2 end, bool overlap, uint mask)
//         {
//             if(mask == 0) return;
//             ErEngine.Renderer.DebugDrawLine(overlap ? ErColor.Red : ErColor.Blue, start, end);
//         }
//         PhysicsWorld = new(new(8, 8), TileSize, 0, tileMaskLookup)
//         {
//             DebugDrawRect = debugDrawRect,
//             DebugDrawLine = debugDrawLine,
//         };
//         Foliage = new();
//         CommandHandler.AddHandler("map_set_tile_rect", HandleSetTileRect);
//         CommandHandler.AddHandler("map_fill_area", TryHandleFillArea);
//         CommandHandler.AddHandler("map_unload_object", HandleUnloadObject);
//     }
//     private void HandleSetTileRect(PriNode command)
//     {
//         var pos = (ErVec2I)SwPrion.GetVec2(command);
//         var size = (ErVec2I)SwPrion.GetVec2(command, "w", "h");
//         int tileId = command.TryGet("tile_id", out int id) ? id : -1;
//         int layerIdx = command.TryGet("layer_idx", out id) ? id : NumTileLayers - 1;
//         ErRect2I rect = new(pos, size);
//         foreach (var coord in rect.GetInnerCoords())
//         {
//             SetTile(layerIdx, coord, tileId);
//         }
//     }
//     public bool InSameRoom(ErVec2 pointA, ErVec2 pointB)
//     {
//         if(!TryGetRoom(pointA, out var roomA)) return false;
//         if(!TryGetRoom(pointB, out var roomB)) return false;
//         return roomA.Id == roomB.Id;
//     }
//     public void AddGlobalObject(SwMapObject mapObject)
//     {
//         GlobalMapObjects.AddObject(mapObject);
//     }
//     private bool TryGetSector(out SwSector sector, ErVec2I tileCoord)
//     {
//         sector = null!;
//         var sectorCoord = tileCoord / SectorSizeTiles;
//         if(LastSector is null || LastSector.PositionSectors != sectorCoord)
//         {
//             if(!SectorLookup.TryGetValue(sectorCoord, out sector!)) return false;
//             else LastSector = sector;
//         }
//         else sector = LastSector;
//         return true;
//     }
//     private SwSector GetSector(ErVec2I tileCoord)
//     {
//         var sectorCoord = tileCoord / SectorSizeTiles;
//         if(LastSector is null || LastSector.PositionSectors != sectorCoord)
//         {
//             if(!SectorLookup.TryGetValue(sectorCoord, out var sector))
//             {
//                 sector = new(sectorCoord, SectorSizeTiles, NumTileLayers);
//                 SectorLookup[sectorCoord] = sector;
//             }
//             LastSector = sector;
//         }
//         return LastSector;
//     }
//     public int GetTile(int layerIdx, ErVec2I tileCoord)
//     {
//         if(!TryGetSector(out var sector, tileCoord)) return -2;
//         return sector.GetTile(layerIdx, tileCoord);
//     }
//     public int GetTopTile(ErVec2I tileCoord)
//     {
//         if(!TryGetSector(out var sector, tileCoord)) return -1;
//         return sector.GetTopTile(tileCoord);
//     }
//     public void SetTile(int layerIdx, ErVec2I tileCoord, int tileId)
//     {
//         var sector = GetSector(tileCoord);
//         sector.SetTile(layerIdx, tileCoord, tileId);
//         int topTileId = sector.GetTopTile(tileCoord);
//         PhysicsWorld.SetTile(tileCoord, topTileId);
//         DisplayLayers[layerIdx].SetTile(tileCoord, tileId);
//     }
//     private void AddRoom(SwRoom room)
//     {
//         Rooms.Add(room.Id, room);
//     }
//     public void Update()
//     {
//         CommandHandler.Dispatch();
//         foreach (var item in GlobalMapObjects.GetObjects())
//         {
//             item.Update();
//         }
//         foreach (var room in LoadedRooms.Values)
//         {
//             room.Update();
//         }
//     }
//     public void Draw()
//     {
//         foreach (var layer in DisplayLayers)
//         {
//             layer.Draw();
//         }
//         Foliage.Draw();
//         foreach (var room in LoadedRooms.Values)
//         {
//             room.Draw();
//         }
//         foreach (var item in GlobalMapObjects.GetObjects())
//         {
//             item.Draw();
//         }
//     }
//     private void TryHandleFillArea(PriNode command)
//     {
//         if(!command.TryGet("area_id", out string area_id))
//         {
//             ErEngine.LogWarning("missing area id");
//             return;
//         }
//         if(!GlobalMapObjects.TryGetObject<SwMapArea>(area_id, out var area))
//         {
//             ErEngine.LogWarning("no such area id ", area_id);
//             return;
//         }
//         if(!command.TryGet("layer_idx", out int layerIdx)) layerIdx = 0;
//         layerIdx = NumTileLayers - 1 - layerIdx;
//         if(!command.TryGet("tile_id", out int tile_id)) tile_id = -1;
//         foreach (var coord in area.RectTiles.GetInnerCoords())
//         {
//             SetTile(layerIdx, coord, tile_id);
//         }
//     }
//     public bool TryGetDefaultCheckpoint(out SwMapCheckpoint checkpoint)
//     {
//         checkpoint = null!;
//         foreach (var item in GlobalMapObjects.GetObjects<SwMapCheckpoint>())
//         {
//             if(!item.Fields.TryGet("default", out bool isDefault) || !isDefault) continue;
//             if(checkpoint is null) checkpoint = item;
//             else ErEngine.LogWarning("duplicate default checkpoints found");
//         }
//         return checkpoint is not null;
//     }
//     public bool TryGetRoom(ErVec2 position, out SwRoom room)
//     {
//         ErVec2I sectorCoord = (position/(ErVec2)SectorSizePx).FloorToInt();
//         return RoomLookup.TryGetValue(sectorCoord, out room!);
//     }
//     private void HandleUnloadObject(PriNode command)
//     {
//         if(!command.TryGet("id", out string id)) return;
//         GlobalMapObjects.Unload(id);
//     }
//     private void LoadRoom(SwRoom room)
//     {
//         Rooms.TryAdd(room.Id, room);
//         LoadedRooms.Add(room.Id, room);
//         foreach (var (key, tileId) in room.TileLookup)
//         {
//             SetTile(key.layerIdx, key.tileCoord, tileId);
//         }
//         foreach (var sectorCoord in room.SectorCoords)
//         {
//             RoomLookup.Add(sectorCoord, room);
//             var sector = GetSector(sectorCoord * SectorSizeTiles);
//             foreach (var item in sector.RectTiles.GetInnerCoords())
//             {
//                 int tileId = sector.GetTopTile(item);
//                 if(tileId > 0 && SwGame.TileData[tileId].IsArable) Foliage.SetArable(item, true);
//             }
//         }
//         room.LoadObjects();
//     }
//     public bool TryLoadRoom(string roomId)
//     {
//         if(!Rooms.TryGetValue(roomId, out var room)) return false;
//         LoadRoom(room);
//         return true;
//     }
//     public void DebugLoadAllRooms()
//     {
//         foreach (var room in Rooms.Values)
//         {
//             LoadRoom(room);
//         }
//         Foliage.LifeSimTrim();
//     }
//     public void LoadGlobals()
//     {
//         foreach (var item in GlobalMapObjects.GetObjects())
//         {
//             item.Load();
//         }
//     }
//     public static bool TryFromData(string filepath, PriNode data, SwTileData[] tileData, out SwMap map)
//     {
//         map = null!;
//         if(!data.Get("iid").TryAs(out string id)) return false;
//         if(!data.Get("levels").TryAs(out PriList rooms)) return false;
//         if(!data.Get("defs").Get("layers").TryAs(out PriList layers)) return false;
//         if(!data.Get("defaultGridSize").TryAs(out int defaultGridSize)) defaultGridSize = 32;
//         if(!data.Get("worldGridWidth").TryAs(out int sectorWidthPx)) sectorWidthPx = 640;
//         if(!data.Get("worldGridHeight").TryAs(out int sectorHeightPx)) sectorHeightPx = 320;
//         ErVec2I tileSize = new(defaultGridSize, defaultGridSize);
//         int numTileLayers = 0;
//         foreach (var layerData in layers.Values)
//         {
//             if(!layerData.Get("type").TryAs(out string layerType)) return ErEngine.LogWarning("malformed layer: ", layerData);
//             if(layerType == "Tiles") numTileLayers++;
//         }
//         map = new(Path.GetDirectoryName(filepath)!, id, numTileLayers, tileSize, new(sectorWidthPx,sectorHeightPx), tileData);
//         foreach (var roomData in rooms.Values)
//         {
//             if(SwRoom.TryFromData(map, roomData, out var room)) map.AddRoom(room);
//             else return ErEngine.LogWarning("malformed room");
//         }
//         map.Foliage.LifeSimTrim();
//         return true;
//     }
// }
