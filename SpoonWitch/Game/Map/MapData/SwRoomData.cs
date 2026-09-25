using ErisMath;

namespace SpoonWitch.Game.Map.MapData;

public class SwRoomData
{
    public string Iid{get; init;} = string.Empty;
    public ErRect2I RectSectors{get; init;}
    public ErRect2I RectTiles{get; init;}
    public ErRect2I RectPx{get; init;}
    public readonly HashSet<string> ObjectIds = [];
}
