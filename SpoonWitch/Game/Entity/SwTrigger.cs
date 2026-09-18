using Eris;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity;

public class SwTrigger : SwEntity
{
    private readonly SwAreaComponent Area;
    public SwTrigger()
    {
        Area = new(this, "area", 2, new(32, 32), enabled: true, onBodyEnter: OnEnter);
        RegisterComponent(Area);
    }
    public override void Ready()
    {
        base.Ready();
        var size = SwPrion.GetVec2(Props.Data, "width_px", "height_px");
        Area.Size = size;
        if(Props.TryGet("mask", out uint mask)) Area.Mask = mask;
    }
    private void OnEnter(SwEntity entity)
    {
        if(!Props.TryGet("on_enter_json", out PriNode command)) return;
        if(Props.TryGet("is_command_global", out bool b) && b) SwApp.CommandStore.AddCommand(command);
        else entity.AddCommand(command);
        if(Props.TryGet("single_use", out bool single_use) && single_use) QueueFree();
    }
}
