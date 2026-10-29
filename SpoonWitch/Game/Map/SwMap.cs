using Eris;
using Eris.Utils.Grid2D;
using ErisMath;
using Prion.Node;
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
    private readonly HashSet<ErVec2I> TileUpdateQueue = [];
    private readonly ErHashGrid2D<SwSector> SectorGrid;
    private readonly Queue<Action> PostprocessQueue = [];
    private readonly List<(string verb, Action<PriNode> handler)> GlobalHandlers = [];
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
        AddGlobalHandler("map_set_tiles_rect", HandleSetTilesRect);
        AddGlobalHandler("map_set_tiles_area", HandleSetTilesArea);
        AddGlobalHandler("map_set_checkpoint", HandleSetCheckpoint);
        AddGlobalHandler("map_bollard_enable", HandleBollardEnable);
        AddGlobalHandler("map_bollard_disable", HandleBollardDisable);
    }
    public bool IsPointInRoom(string roomId, ErVec2 point)
    {
        var sectorCoord = point.FloorToInt() / SectorSizePx;
        if(!RoomSectorLookup.TryGetValue(sectorCoord, out string? rid)) return false;
        return rid == roomId;
    }
    public bool TryGetRoomRect(string roomId, out ErRect2 roomRect)
    {
        roomRect = default;
        if(!RoomIdLookup.TryGetValue(roomId, out int roomIdx)) return false;
        roomRect = (ErRect2)(MapData.Rooms[roomIdx].RectSectors * SectorSizePx);
        return true;
    }
    private void AddGlobalHandler(string verb, Action<PriNode> handler)
    {
        SwApp.CommandQueue.AddHandler(verb, handler);
        GlobalHandlers.Add((verb, handler));
    }
    private void HandleSetTilesRect(PriNode command)
    {
        if(!command.TryGet("layer_idx", out int layerIdx)) {ErEngine.LogWarning("set tiles command missing layer_idx"); return;}
        if(!SwPrion.TryGetRect2I(out var tileRect, command.Get("rect_tiles"))) {ErEngine.LogWarning("set tiles command missing rect_tiles"); return;}
        if(!command.TryGet("tile_id", out int tileId)) {ErEngine.LogWarning("set tiles command missing tile_id"); return;}
        SetTilesRect(layerIdx, tileRect, tileId);
    }
    private void HandleSetTilesArea(PriNode command)
    {
        if(!command.TryGet("layer_idx", out int layerIdx)) {ErEngine.LogWarning("set tiles command missing layer_idx"); return;}
        if(!command.TryGet("area_iid", out string areaIid)) {ErEngine.LogWarning("set tiles command missing area_iid"); return;}
        if(!command.TryGet("tile_id", out int tileId)) {ErEngine.LogWarning("set tiles command missing tile_id"); return;}
        if(!MapObjectIdLookup.TryGetValue(areaIid, out int mapObjectIdx)) {ErEngine.LogWarning("no such area iid ", areaIid); return;}
        string className = MapData.Objects[mapObjectIdx].Class;
        if(className != "area") {ErEngine.LogWarning("iid does not point to an area, it is a ", className); return;}
        var tileRect = MapData.Objects[mapObjectIdx].RectTiles;
        SetTilesRect(layerIdx, tileRect, tileId);
    }
    private void HandleSetCheckpoint(PriNode command)
    {
        if(!command.TryGet("iid", out string iid))
        {
            ErEngine.LogWarning("command missing iid field");
            return;
        }
        if(!MapObjectIdLookup.TryGetValue(iid, out int mapObjectIdx))
        {
            ErEngine.LogWarning("no map object with iid '", iid, "' exists");
            return;
        }
        var mapObject = MapData.Objects[mapObjectIdx];
        if(mapObject.Class != "checkpoint")
        {
            ErEngine.LogWarning("map object with iid '", iid, "' is not a checkpoint, it is a ", mapObject.Class);
            return;
        }
        CurrentCheckpoint = mapObject;
    }
    private void HandleBollardEnable(PriNode command)
    {
        if(!command.TryGet("iid", out string iid)) ErEngine.LogWarning("no bollard id");
        Game.Flags[iid] = 1;
        SwApp.CommandQueue.AddCommandVerb("bollard_update");
    }
    private void HandleBollardDisable(PriNode command)
    {
        if(!command.TryGet("iid", out string iid)) ErEngine.LogWarning("no bollard id");
        Game.Flags[iid] = 0;
        SwApp.CommandQueue.AddCommandVerb("bollard_update");
    }
    private void SetTilesRect(int layerIdx, ErRect2I tileRect, int tileId)
    {
        foreach (var tileCoord in tileRect.GetInnerCoords())
        {
            SetTile(layerIdx, tileCoord, tileId);
        }
        UpdateTiles();
    }
    private void SetTilesSpans(IEnumerable<SwMapTileSpan> spans)
    {
        foreach (var span in spans)
        {
            for (int idx = 0; idx < span.Length; idx++)
            {
                ErVec2I offset = new(idx, 0);
                var tileCoord = span.TileCoord + offset;
                SetTile(span.LayerIdx, tileCoord, span.TileId);
            }
        }
        UpdateTiles();
    }
    private void SetTile(int layerIdx, ErVec2I tileCoord, int tileId)
    {
        var sectorCoord = tileCoord / SectorSizeTiles;
        SectorGrid.Get(sectorCoord, NewSector).SetTile(layerIdx, tileCoord, tileId);
        TileUpdateQueue.Add(tileCoord);
    }
    private void UpdateTiles()
    {
        foreach (var tileCoord in TileUpdateQueue)
        {
            int topTileId = GetTopTileId(tileCoord);
            uint mask = uint.MaxValue;
            if(topTileId == -1) mask = 0;
            else if(topTileId >= 0) mask = TileData[topTileId].CollisionMask;
            Game.PhysicsWorld.SetTileMask(tileCoord, mask);
            MapDisplay.QueueTileUpdate(tileCoord);
            Foliage.QueueTileUpdate(tileCoord);
        }
        TileUpdateQueue.Clear();
    }
    public void Update(ErVec2 targetPoint)
    {
        var sectorCoord = (ErVec2I)targetPoint / SectorSizePx;
        HandleLoading(sectorCoord);
    }
    public void Draw()
    {
        MapDisplay.Draw();
        Foliage.Draw();
    }
    public void Cleanup()
    {
        // unload all rooms
        foreach (var id in LoadedRooms.Keys)
        {
            UnloadRoom(id);
        }
        // unload any remaining objects
        foreach (var id in LoadedMapObjects.Keys)
        {
            UnloadMapObject(id);
        }
        // remove handlers
        foreach (var (verb,action) in GlobalHandlers)
        {
            SwApp.CommandQueue.RemoveHandler(verb, action);
        }
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
    public bool TryGetRoomId(ErVec2 point, out string roomId)
    {
        roomId = string.Empty;
        var sectorCoord = (ErVec2I)point / SectorSizePx;
        if(!RoomSectorLookup.TryGetValue(sectorCoord, out var id)) return false;
        roomId = id;
        return true;
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
        if(CurrentRoom is not null) UnloadMapObjects(CurrentRoom.Data);
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
        // load contents of new current room
        LoadMapObjects(CurrentRoom.Data);
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
        SetTilesSpans(room.Data.TileSpans);
        // load map objects
        // LoadMapObjects(room.Data);
        // foreach (var mapObjectId in room.Data.ObjectIds)
        // {
        //     LoadMapObject(mapObjectId);
        // }
        // enqueue postprocessing
        PostprocessQueue.Enqueue(()=>HandleFoliage(room.RectTiles));
        // ErEngine.Log("loaded room: ", room.Data.Name);
        // string roomDiffPath = $"maps/{MapData.Iid}/room_diffs/{roomId}";
        // if(SwData.SaveData.TryGet(roomDiffPath, out PriList diffs))
        // {
        //     foreach (var item in diffs.Values)
        //     {
        //         SwApp.CommandQueue.AddCommand(item);
        //     }
        // }
        return room;
    }
    private void LoadMapObjects(SwRoomData roomData)
    {
        foreach (var mapObjectId in roomData.ObjectIds)
        {
            LoadMapObject(mapObjectId);
        }
    }
    private void UnloadMapObjects(SwRoomData roomData)
    {
        foreach (var mapObjectId in roomData.ObjectIds)
        {
            UnloadMapObject(mapObjectId);
        }
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
        // UnloadMapObjects(room.Data);
        // foreach (var mapObjectId in room.Data.ObjectIds)
        // {
        //     UnloadMapObject(mapObjectId);
        // }
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
        props.TrySet("map_iid", MapData.Iid);
        // ErEngine.Log(props);
        string className = MapData.Objects[mapObjectIdx].Class;
        int entId;// int.MaxValue;
        switch (className)
        {
            case "area":
                // areas do not have a map entity, so their id is -1
                entId = -1;
                break;
            case "prop":
                entId = Game.AddEntity<SwProp>(props).Id;
                break;
            case "checkpoint":
                entId = Game.AddEntity<SwCheckpoint>(props).Id;
                break;
            case "spawner":
                entId = Game.AddEntity<SwSpawner>(props).Id;
                break;
            case "pickup":
                entId = Game.AddEntity<SwPickup>(props).Id;
                break;
            case "trigger":
                entId = Game.AddEntity<SwTrigger>(props).Id;
                break;
            case "fwoosher":
                entId = Game.AddEntity<SwFwoosher>(props).Id;
                break;
            case "bollard":
                entId = Game.AddEntity<SwBollard>(props).Id;
                break;
            case "sign":
                entId = Game.AddEntity<SwSign>(props).Id;
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
    private SwSector NewSector(ErVec2I sectorCoord)
    {
        return new(sectorCoord, SectorSizeTiles, MapData.NumTileLayers);
    }
}
