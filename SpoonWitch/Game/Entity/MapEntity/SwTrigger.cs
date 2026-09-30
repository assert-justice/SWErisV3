using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.MapEntity;

public class SwTrigger : SwMapEntity
{
    private SwAreaComponent Area = null!;
    public SwTrigger()
    {
    }
    public override void SetProps(PriNode props)
    {
        base.SetProps(props);
    }
    public override void Init()
    {
        base.Init();
        ErEngine.Log(Props);
        var size = SwPrion.GetVec2(Props.Data.Get("rect_px"), "w", "h", new ErVec2(32,32));
        ErEngine.Log(Position, " ", size);
        if(!Props.TryGet("mask", out uint mask)) mask = 2;
        Area = new(this, "area", mask, size, enabled: true, onBodyEnter: OnEnter);
        RegisterComponent(Area);
    }
    private void OnEnter(SwEntity entity)
    {
        ErEngine.Log("here");
        // if(!Props.TryGet("on_enter_json", out PriNode command)) return;
        // if(Props.TryGet("is_command_global", out bool b) && b) SwApp.CommandStore.AddCommand(command);
        // else entity.AddCommand(command);
        // if(Props.TryGet("single_use", out bool single_use) && single_use) QueueFree();
    }
}
