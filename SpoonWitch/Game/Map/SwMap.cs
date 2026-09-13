using Eris;
using Eris.Renderer;
using ErisMath;
using ErisPhysics2D;
using Prion.Node;
using SpoonWitch.Command;
using SpoonWitch.Game.Map.Foliage;
using SpoonWitch.Game.Map.MapObject;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Map;

public class SwMap
{
    private readonly Dictionary<string,SwRoom> Rooms = [];
    private readonly Dictionary<string,SwRoom> LoadedRooms = [];
    private readonly Dictionary<ErVec2I, SwSector> SectorLookup = [];
    private readonly Dictionary<ErVec2I,SwRoom> RoomLookup = [];
    // private readonly SwTileData[] TileData;
    private readonly SwDisplayLayer[] DisplayLayers;
    public readonly int NumTileLayers;
    public readonly ErPhysicsWorld2D PhysicsWorld;
    public readonly SwFoliage Foliage;
    public readonly string Id;
    public readonly ErVec2I TileSize;
    public readonly ErVec2I SectorSizeTiles;
    public readonly ErVec2I SectorSizePx;
    private readonly SwMapObjectLookup GlobalMapObjects = new();
    private readonly SwCommandHandler CommandHandler = new(SwApp.CommandStore);
    public readonly string Dirpath;
    private SwSector? LastSector;
    public SwMap(string dirpath = "", string id = "", int numTileLayers = 0, ErVec2I? tileSize = null, ErVec2I? sectorSizePx = null, SwTileData[]? tileData = null)
    {
        Dirpath = dirpath;
        Id = id;
        NumTileLayers = numTileLayers;
        DisplayLayers = new SwDisplayLayer[numTileLayers];
        for (int i = 0; i < DisplayLayers.Length; i++)
        {
            DisplayLayers[i] = new(this);
        }
        TileSize = tileSize ?? new(32, 32);
        SectorSizePx = sectorSizePx ?? new(640, 320);
        SectorSizeTiles = SectorSizePx / TileSize;
        var TileData = tileData ?? [];
        uint[] tileMaskLookup = [..TileData.Select(t => t.CollisionMask)];
        static void debugDrawRect(ErRect2 rect, bool overlap, uint mask)
        {
            if(mask == 0) return;
            ErEngine.Renderer.DebugDrawRect(overlap ? ErColor.Red : ErColor.Blue, rect, false);
        }
        static void debugDrawLine(ErVec2 start, ErVec2 end, bool overlap, uint mask)
        {
            if(mask == 0) return;
            ErEngine.Renderer.DebugDrawLine(overlap ? ErColor.Red : ErColor.Blue, start, end);
        }
        PhysicsWorld = new(new(8, 8), TileSize, 0, tileMaskLookup)
        {
            DebugDrawRect = debugDrawRect,
            DebugDrawLine = debugDrawLine,
        };
        Foliage = new();
        CommandHandler.AddHandler("map_set_tile_rect", HandleSetTileRect);
        CommandHandler.AddHandler("map_fill_area", TryHandleFillArea);
        CommandHandler.AddHandler("map_unload_object", HandleUnloadObject);
    }
    private void HandleSetTileRect(PriNode command)
    {
        var pos = (ErVec2I)SwPrion.GetVec2(command);
        var size = (ErVec2I)SwPrion.GetVec2(command, "w", "h");
        int tileId = command.TryGet("tile_id", out int id) ? id : -1;
        int layerIdx = command.TryGet("layer_idx", out id) ? id : NumTileLayers - 1;
        ErRect2I rect = new(pos, size);
        foreach (var coord in rect.GetInnerCoords())
        {
            SetTile(layerIdx, coord, tileId);
        }
    }
    public bool InSameRoom(ErVec2 pointA, ErVec2 pointB)
    {
        if(!TryGetRoom(pointA, out var roomA)) return false;
        if(!TryGetRoom(pointB, out var roomB)) return false;
        return roomA.Id == roomB.Id;
    }
    public void AddGlobalObject(SwMapObject mapObject)
    {
        GlobalMapObjects.AddObject(mapObject);
    }
    private bool TryGetSector(out SwSector sector, ErVec2I tileCoord)
    {
        sector = null!;
        var sectorCoord = tileCoord / SectorSizeTiles;
        if(LastSector is null || LastSector.PositionSectors != sectorCoord)
        {
            if(!SectorLookup.TryGetValue(sectorCoord, out sector!)) return false;
            else LastSector = sector;
        }
        return true;
    }
    private SwSector GetSector(ErVec2I tileCoord)
    {
        var sectorCoord = tileCoord / SectorSizeTiles;
        if(LastSector is null || LastSector.PositionSectors != sectorCoord)
        {
            if(!SectorLookup.TryGetValue(sectorCoord, out var sector))
            {
                sector = new(sectorCoord, SectorSizeTiles, NumTileLayers);
            }
            LastSector = sector;
        }
        return LastSector;
    }
    public int GetTile(int layerIdx, ErVec2I tileCoord)
    {
        if(!TryGetSector(out var sector, tileCoord)) return -1;
        return sector.GetTile(layerIdx, tileCoord);
    }
    public void SetTile(int layerIdx, ErVec2I tileCoord, int tileId)
    {
        var sector = GetSector(tileCoord);
        sector.SetTile(layerIdx, tileCoord, tileId);
        int topTileId = sector.GetTopTile(tileCoord);
        PhysicsWorld.SetTile(tileCoord, topTileId);
        DisplayLayers[layerIdx].SetTile(tileCoord, tileId);
    }
    private void AddRoom(SwRoom room)
    {
        Rooms.Add(room.Id, room);
    }
    public void Update()
    {
        CommandHandler.Dispatch();
        foreach (var item in GlobalMapObjects.GetObjects())
        {
            item.Update();
        }
        foreach (var room in LoadedRooms.Values)
        {
            room.Update();
        }
    }
    public void Draw()
    {
        foreach (var layer in DisplayLayers)
        {
            layer.Draw();
        }
        Foliage.Draw();
        foreach (var room in LoadedRooms.Values)
        {
            room.Draw();
        }
        foreach (var item in GlobalMapObjects.GetObjects())
        {
            item.Draw();
        }
    }
    private void TryHandleFillArea(PriNode command)
    {
        if(!command.TryGet("area_id", out string area_id))
        {
            ErEngine.LogWarning("missing area id");
            return;
        }
        if(!GlobalMapObjects.TryGetObject<SwMapArea>(area_id, out var area))
        {
            ErEngine.LogWarning("no such area id ", area_id);
            return;
        }
        if(!command.TryGet("layer_idx", out int layerIdx)) layerIdx = 0;
        layerIdx = NumTileLayers - 1 - layerIdx;
        if(!command.TryGet("tile_id", out int tile_id)) tile_id = -1;
        foreach (var coord in area.RectTiles.GetInnerCoords())
        {
            SetTile(layerIdx, coord, tile_id);
        }
    }
    public bool TryGetDefaultCheckpoint(out SwMapCheckpoint checkpoint)
    {
        checkpoint = null!;
        foreach (var item in GlobalMapObjects.GetObjects<SwMapCheckpoint>())
        {
            if(!item.Fields.TryGet("default", out bool isDefault) || !isDefault) continue;
            if(checkpoint is null) checkpoint = item;
            else ErEngine.LogWarning("duplicate default checkpoints found");
        }
        return checkpoint is not null;
    }
    public bool TryGetRoom(ErVec2 position, out SwRoom room)
    {
        ErVec2I sectorCoord = (position/(ErVec2)SectorSizePx).FloorToInt();
        return RoomLookup.TryGetValue(sectorCoord, out room!);
    }
    private void HandleUnloadObject(PriNode command)
    {
        if(!command.TryGet("id", out string id)) return;
        GlobalMapObjects.Unload(id);
    }
    private void LoadRoom(SwRoom room)
    {
        Rooms.TryAdd(room.Id, room);
        LoadedRooms.Add(room.Id, room);
        foreach (var (key, tileId) in room.TileLookup)
        {
            SetTile(key.layerIdx, key.tileCoord, tileId);
        }
        foreach (var sectorCoord in room.SectorCoords)
        {
            RoomLookup.Add(sectorCoord, room);
            var sector = GetSector(sectorCoord * SectorSizeTiles);
            foreach (var item in sector.RectTiles.GetInnerCoords())
            {
                int tileId = sector.GetTopTile(item);
                if(tileId > 0 && SwGame.TileData[tileId].IsArable) Foliage.SetArable(item, true);
            }
        }
        room.LoadObjects();
    }
    public bool TryLoadRoom(string roomId)
    {
        if(!Rooms.TryGetValue(roomId, out var room)) return false;
        LoadRoom(room);
        return true;
    }
    public void DebugLoadAllRooms()
    {
        foreach (var room in Rooms.Values)
        {
            LoadRoom(room);
        }
        Foliage.LifeSimTrim();
    }
    public void LoadGlobals()
    {
        foreach (var item in GlobalMapObjects.GetObjects())
        {
            item.Load();
        }
    }
    public static bool TryFromData(string filepath, PriNode data, SwTileData[] tileData, out SwMap map)
    {
        map = null!;
        if(!data.Get("iid").TryAs(out string id)) return false;
        if(!data.Get("levels").TryAs(out PriList rooms)) return false;
        if(!data.Get("defs").Get("layers").TryAs(out PriList layers)) return false;
        if(!data.Get("defaultGridSize").TryAs(out int defaultGridSize)) defaultGridSize = 32;
        if(!data.Get("worldGridWidth").TryAs(out int sectorWidthPx)) sectorWidthPx = 640;
        if(!data.Get("worldGridHeight").TryAs(out int sectorHeightPx)) sectorHeightPx = 320;
        ErVec2I tileSize = new(defaultGridSize, defaultGridSize);
        int numTileLayers = 0;
        foreach (var layerData in layers.Values)
        {
            if(!layerData.Get("type").TryAs(out string layerType)) return ErEngine.LogWarning("malformed layer: ", layerData);
            if(layerType == "Tiles") numTileLayers++;
        }
        map = new(Path.GetDirectoryName(filepath)!, id, numTileLayers, tileSize, new(sectorWidthPx,sectorHeightPx), tileData);
        foreach (var roomData in rooms.Values)
        {
            if(SwRoom.TryFromData(map, roomData, out var room)) map.AddRoom(room);
            else return ErEngine.LogWarning("malformed room");
        }
        map.Foliage.LifeSimTrim();
        return true;
    }
}
