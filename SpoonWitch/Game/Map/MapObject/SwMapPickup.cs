using Eris;
using Prion.Node;
using SpoonWitch.Data;
using SpoonWitch.Game.Entity;

namespace SpoonWitch.Game.Map.MapObject;

public class SwMapPickup : SwMapObject
{
    public SwMapPickup(PriNode data) : base(data){}
    public override void Load()
    {
        base.Load();
        if(!GetProps().TryAs(out PriDict props)) throw new("should be unreachable");
        if(!Fields.TryGet("pickup_type", out string pickup_type))
        {
            ErEngine.LogWarning("bad pickup");
            return;
        }
        if(pickup_type == "none") return;
        props.Merge(SwData.Prototypes.Get($"pickups/{pickup_type}"));
        // ErEngine.Log(GetProps());
        SwGame.Game.LoadEntity<SwPickup>(props);
    }
}
