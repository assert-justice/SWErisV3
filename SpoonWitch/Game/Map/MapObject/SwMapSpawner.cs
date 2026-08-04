using ErisMath;
using Prion.Node;

namespace SpoonWitch.Game.Map.MapObject;

public class SwMapSpawner : SwMapObject
{
    // public SwMapSpawner(SwRoom room, string id, ErRect2I rect, PriDict data) : base(room, id, rect, "spawner", data)
    // {
    // }
    public SwMapSpawner(SwRoom room, PriNode node) : base(room, node)
    {
    }
}