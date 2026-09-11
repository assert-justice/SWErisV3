using Eris;
using Eris.Renderer;
using ErisMath;
using ErisPhysics2D;
using Prion.Node;
using SpoonWitch.Game.Map.Foliage;
using SpoonWitch.Game.Map.MapObject;

namespace SpoonWitch.Game.Map;

public class SwMap
{
    private readonly Dictionary<string,SwRoom> Rooms = [];
    private readonly Dictionary<string,SwRoom> LoadedRooms = [];
    private readonly Dictionary<ErVec2I, SwRoom> SectorLookup = [];
    private readonly SwTileData[] TileData;
    private readonly SwDisplayLayer[] DisplayLayers;
    public readonly int NumTileLayers;
    public readonly ErPhysicsWorld2D PhysicsWorld;
    public readonly SwFoliage Foliage;
    public readonly string Id;
    public readonly ErVec2I TileSize;
    public readonly ErVec2I SectorSizeTiles;
    public readonly ErVec2I SectorSizePx;
    private readonly SwMapObjectLookup GlobalMapObjects = new();
    public readonly string Dirpath;
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
        TileData = tileData ?? [];
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
    }
    public void AddGlobalObject(SwMapObject mapObject)
    {
        GlobalMapObjects.AddObject(mapObject);
    }
    public SwTileData GetTileData(int tileId)
    {
        return TileData[tileId];
    }
    public void SetTile(int layer, ErVec2I coord, int tileId, bool updateFoliage = false)
    {
        if(updateFoliage) Foliage.SetArable(coord, TileData[tileId].IsArable);
        PhysicsWorld.SetTile(coord, tileId);
        DisplayLayers[layer].SetTile(coord, tileId);
    }
    private void AddRoom(SwRoom room)
    {
        foreach (var sector in room.GetSectors())
        {
            SectorLookup.Add(sector.PositionSectors,room);
        }
        LoadRoom(room);
    }
    public void Update()
    {
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
    }
    private bool TryHandleFillArea(PriNode command)
    {
        if(!command.TryGet("area_id", out string area_id)) return ErEngine.LogWarning("missing area id");
        if(!GlobalMapObjects.TryGetObject<SwMapArea>(area_id, out var area)) return ErEngine.LogWarning("no such area id ", area_id);
        if(!command.TryGet("layer", out int layerIdx)) layerIdx = 0;
        layerIdx = NumTileLayers - 1 - layerIdx;
        if(!command.TryGet("tile_id", out int tile_id)) tile_id = -1;
        foreach (var coord in area.RectTiles.GetInnerCoords())
        {
            SetTile(layerIdx, coord, tile_id);
        }
        return true;
    }
    public void HandleCommands()
    {
        foreach (var item in SwApp.CommandStore.GetGlobalCommands("map_fill_area"))
        {
            TryHandleFillArea(item);
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
        ErVec2I sector = (position/(ErVec2)SectorSizePx).FloorToInt();
        return SectorLookup.TryGetValue(sector, out room!);
    }
    private void LoadRoom(SwRoom room)
    {
        Rooms.TryAdd(room.Id, room);
        LoadedRooms.Add(room.Id, room);
        room.Load();
    }
    public bool TryLoadRoom(string roomId)
    {
        if(!Rooms.TryGetValue(roomId, out var room)) return false;
        LoadRoom(room);
        return true;
    }
    public void LoadGlobals()
    {
        foreach (var item in GlobalMapObjects.GetObjects())
        {
            item.Load();
        }
    }
    // public void UnloadRoom(string roomId)
    // {
    //     if(!LoadedRooms.TryGetValue(roomId, out var room))
    //     {
    //         ErEngine.LogWarning("no room with id '", roomId, "' is loaded.");
    //         return;
    //     }
    //     room.Unload();
    //     LoadedRooms.Remove(roomId);
    // }
    public static bool TryFromData(string filepath, PriNode data, SwTileData[] tileData, out SwMap map)
    {
        map = null!;
        if(!data.Get("iid").TryAs(out string id)) return false;
        if(!data.Get("levels").TryAs(out PriList rooms)) return false;
        if(!data.Get("defs").Get("tilesets").TryAs(out PriList tilesetList)) return false;
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
