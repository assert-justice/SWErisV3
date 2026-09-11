using Eris;
using ErisPhysics2D.Collider;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Map.Collision;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity;

public class SwTrigger : SwEntity, ISwEntity<SwTrigger>
{
    public static byte TypeId => 4;
    public override uint Mask => 0;
    private static SwTrigger? _Primary;
    private static SwTrigger? _Secondary;
    public static SwTrigger Primary => _Primary ??= new();
    public static SwTrigger Secondary => _Secondary ??= new();
    protected override byte GetTypeId => TypeId;
    private readonly SwAreaComponent Area;
    public SwTrigger()
    {
        Area = new(this, "area", 2, new(32, 32))
        {
            Enabled = true,
        };
        Area.Area.OnBodyEnterFn = OnEnter;
        RegisterComponent(Area);
    }
    public override void Ready()
    {
        base.Ready();
        var size = SwPrion.GetVec2(EntProps.Props.Data, "width_px", "height_px");
        Area.Size = size;
        if(EntProps.Props.TryGet("mask", out uint mask)) Area.Mask = mask;
    }
    private static void OnEnter(SwColliderArea area, int bodyId, ErColliderBody body)
    {
        if(!SwGame.TryGetEntProps(area.ParentId, out var pickupProps)) return;
        if(!pickupProps.Props.TryGet("on_enter_json", out PriNode command)) return;
        if(pickupProps.Props.TryGet("is_command_global", out bool b) && b) SwApp.CommandStore.AddGlobalCommand(command);
        else
        {
            if(!SwGame.TryGetEntProps(body.ParentId, out var targetProps)) return;
            targetProps.AddCommand(command);
        }
    }
}
