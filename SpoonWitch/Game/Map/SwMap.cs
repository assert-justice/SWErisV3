using System.Text.Json.Nodes;
using Eris;
using ErisMath;
using Prion.Node;
using Prion.Parser;
using SpoonWitch.Game.Map.MapObject;

namespace SpoonWitch.Game.Map;

public class SwMap
{
    private readonly Dictionary<string,SwRoom> Rooms = [];
    private readonly Dictionary<string,SwRoom> LoadedRooms = [];
    private readonly Dictionary<ErVec2I, SwRoom> SectorLookup = [];
    private readonly List<SwTileData> TileData = [];
    private readonly SwDisplayLayer[] DisplayLayers;
    public readonly SwCollisionLayer CollisionLayer;
    public readonly string Id;
    public readonly ErVec2I TileSize;
    public readonly ErVec2I SectorSizeTiles;
    public readonly ErVec2I SectorSizePx;
    private readonly Dictionary<string,SwMapObject> GlobalMapObjects = [];
    public SwMap(string id = "", int numTileLayers = 0, ErVec2I? tileSize = null, ErVec2I? sectorSizePx = null)
    {
        Id = id;
        DisplayLayers = new SwDisplayLayer[numTileLayers];
        for (int i = 0; i < DisplayLayers.Length; i++)
        {
            DisplayLayers[i] = new(this);
        }
        TileSize = tileSize ?? new(32, 32);
        SectorSizePx = sectorSizePx ?? new(640, 320);
        SectorSizeTiles = SectorSizePx / TileSize;
        CollisionLayer = new(this);
    }
    public void AddGlobalObject(SwMapObject mapObject)
    {
        GlobalMapObjects.Add(mapObject.Id, mapObject);
    }
    public SwTileData GetTileData(int tileId)
    {
        return TileData[tileId];
    }
    public void SetTile(int layer, ErVec2I coord, int tileId)
    {
        CollisionLayer.SetTile(coord, tileId);
        DisplayLayers[layer].SetTile(coord, tileId);
    }
    private void AddRoom(SwRoom room)
    {
        Rooms.Add(room.Id, room);
        room.Load();
        foreach (var sector in room.GetSectors())
        {
            SectorLookup.Add(sector,room);
        }
    }
    public void Update()
    {
        foreach (var item in GlobalMapObjects.Values)
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
        foreach (var room in LoadedRooms.Values)
        {
            room.Draw();
        }
        if (SwApp.Debug)
        {
            CollisionLayer.DebugDraw();
        }
    }
    public bool TryGetRoom(ErVec2 position, out SwRoom room)
    {
        ErVec2I sector = (position/(ErVec2)SectorSizePx).FloorToInt();
        return SectorLookup.TryGetValue(sector, out room!);
    }
    // public bool TryLoadRoom(string roomId)
    // {
    //     if(!Rooms.TryGetValue(roomId, out var room)) return false;
    //     LoadedRooms.Add(roomId, room);
    //     room.Load();
    //     return true;
    // }
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
    public static bool TryFromData(string filepath, PriNode data, out SwMap map)
    {
        map = null!;
        if(!data.Get("iid").TryAs(out string id)) return false;
        if(!data.Get("levels").TryAs(out PriList rooms)) return false;
        if(!data.Get("defs").Get("tilesets").TryAs(out PriList tilesetList)) return false;
        if(!data.Get("defs").Get("layers").TryAs(out PriList layers)) return false;
        if(!data.Get("defaultGridSize").TryAs(out int defaultGridSize)) defaultGridSize = 32;
        if(!data.Get("worldGridWidth").TryAs(out int sectorWidthPx)) sectorWidthPx = 640;
        if(!data.Get("worldGridHeight").TryAs(out int sectorHeightPx)) sectorHeightPx = 320;
        int numTileLayers = 0;
        foreach (var layerData in layers.Values)
        {
            if(!layerData.Get("type").TryAs(out string layerType)) return ErEngine.LogWarning("malformed layer: ", layerData);
            if(layerType == "Tiles") numTileLayers++;
        }
        map = new(id, numTileLayers, new(defaultGridSize,defaultGridSize), new(sectorWidthPx,sectorHeightPx));
        foreach (var roomData in rooms.Values)
        {
            if(SwRoom.TryFromData(map, roomData, out var room)) map.AddRoom(room);
            else return ErEngine.LogWarning("malformed room");
        }
        foreach (var tileset in tilesetList.Values)
        {
            if(!tileset.Get("identifier").TryAs(out string ident)) continue;
            if(ident != "tile_pallet") continue;
            if(!tileset.Get("customData").TryAs(out PriList tiles)) return ErEngine.LogWarning("no custom data");
            foreach (var t in tiles.Values)
            {
                if(!t.Get("data").TryAs(out string dataStr)) return false;
                try
                {
                    var json = JsonNode.Parse(dataStr);
                    var prion = PriJsonConverter.JsonToPrion(json);
                    if(!SwTileData.TryFromData(filepath, prion, map.TileSize, out var tileData)) return ErEngine.LogWarning("corrupt tile data");
                    map.TileData.Add(tileData);
                }
                catch(Exception e)
                {
                    return ErEngine.LogWarning("json parse failed with error: ", e, " ", dataStr);
                }
            }
            break;
        }
        return true;
    }
}