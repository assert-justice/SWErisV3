using ErisMath;
using Prion.Node;

namespace SpoonWitch.Game.Map.MapObject;

public class SwMapCheckpoint : SwMapObject
{
    // public SwMapCheckpoint(SwRoom room, string id, ErRect2I rect, PriDict data) : base(room, id, rect, "area", data)
    // {
    // }
    public SwMapCheckpoint(SwRoom room, PriNode node) : base(room, node)
    {
    }
}