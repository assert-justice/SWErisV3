using Prion.Node;
using SpoonWitch.Game.Entity;

namespace SpoonWitch.Game.Map.MapObject;

public class SwMapPickup : SwMapObject
{
    public SwMapPickup(PriNode data) : base(data){}
    public override void Load()
    {
        base.Load();
        SwPickup pickup = new();
        pickup.SetProps(GetProps());
        SwGame.Game?.AddEntity(pickup);
    }
}
