using ErisMath;
using Prion.Node;

namespace SpoonWitch.Game.Map.MapObject;

public class SwMapTrigger : SwMapObject
{
    // public SwMapTrigger(SwRoom room, string id, ErRect2I rect, PriDict data) : base(room, id, rect, "trigger", data)
    // {
    // }
    public SwMapTrigger(SwRoom room, PriNode node) : base(room, node)
    {
    }
}