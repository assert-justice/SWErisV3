using Eris;
using ErisPhysics2D.Collider;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Map.Collision;

namespace SpoonWitch.Game.Entity;

public class SwPickup : SwEntity, ISwEntity<SwPickup>
{
    public static byte TypeId => 3;
    private static SwPickup? _Primary;
    private static SwPickup? _Secondary;
    public static SwPickup Primary => _Primary ??= new();
    public static SwPickup Secondary => _Secondary ??= new();
    protected override byte GetTypeId => TypeId;
    public SwPickup()
    {
        SwAreaComponent area = new(this, "spoon_hurtbox", 2, new(32, 32))
        {
            Enabled = true,
        };
        area.Area.OnBodyEnterFn = OnEnter;
        RegisterComponent(area);
    }
    public override void Ready()
    {
        base.Ready();
        PriDict command = [];
        EntProps.Props.TrySet("ent_offer_item", command);
        ErEngine.Log(EntProps.Props);
    }
    private static void OnEnter(SwColliderArea area, int bodyId, ErColliderBody body)
    {
        if(!SwGame.TryGetEntProps(area.ParentId, out var pickupProps)) return;
        if(!SwGame.TryGetEntProps(body.ParentId, out var targetProps)) return;
        if(!pickupProps.Props.TryGet("spoon_damage", out PriNode spoonDamage)) return;
        targetProps.AddCommand(spoonDamage);
    }
}