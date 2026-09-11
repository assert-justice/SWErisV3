using ErisMath;
using Prion.Node;

namespace SpoonWitch.Game.Map.MapObject;

public class SwMapArea : SwMapObject
{
    public SwMapArea(PriNode data) : base(data)
    {
        // At least for now areas are always global
        Data.TrySet("is_global", true);
    }
}