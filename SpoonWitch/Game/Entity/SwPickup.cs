using Eris;
using ErisPhysics2D.Collider;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Map.Collision;

namespace SpoonWitch.Game.Entity;

public class SwPickup : SwEntity, ISwEntity<SwPickup>
{
    public static byte TypeId => 3;
    public override uint Mask => 0;
    private static SwPickup? _Primary;
    private static SwPickup? _Secondary;
    public static SwPickup Primary => _Primary ??= new();
    public static SwPickup Secondary => _Secondary ??= new();
    protected override byte GetTypeId => TypeId;
    private readonly SwAreaComponent Area;
    public SwPickup()
    {
        Area = new(this, "spoon_hurtbox", 2, new(32, 32))
        {
            Enabled = true,
        };
        Area.Area.OnBodyEnterFn = OnEnter;
        RegisterComponent(Area);
        AddHandler("pickup_set_rem", SetRem);
    }
    public override void Ready()
    {
        base.Ready();
        PriDict command = [];
        command.TrySet("verb", "ent_offer_item");
        command.TrySet("pickup_type", EntProps.Props.Get("pickup_type"));
        command.TrySet("count", EntProps.Props.Get("count"));
        command.TrySet("ent_id", Id);
        EntProps.Props.TrySet("ent_offer_item", command);
    }
    private void SetRem(PriNode command)
    {
        if(!command.TryGet("rem", out int rem)) return;
        EntProps.Props.TrySet("count", rem);
        if(rem == 0) Area.Enabled = false;
    }
    private static void OnEnter(SwColliderArea area, int bodyId, ErColliderBody body)
    {
        if(!SwGame.TryGetEntProps(area.ParentId, out var pickupProps)) return;
        if(!SwGame.TryGetEntProps(body.ParentId, out var targetProps)) return;
        if(!pickupProps.Props.TryGet("ent_offer_item", out PriNode command)) return;
        targetProps.AddCommand(command);
    }
}