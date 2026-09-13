using ErisMath;
using Prion.Node;

namespace SpoonWitch.Game.Map.MapObject;

public class SwMapArea : SwMapObject
{
    // At least for now areas are always global
    public override bool IsGlobal => true;
    public SwMapArea(PriNode data) : base(data){}
}