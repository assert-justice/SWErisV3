using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.MapEntity;

public class SwTrigger : SwMapEntity
{
    private SwAreaComponent Area = null!;
    public int Activations;
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
        var size = SwPrion.GetVec2(Props.Data.Get("rect_px"), "w", "h", new ErVec2(32,32));
        if(!Props.TryGet("mask", out uint mask)) mask = 2;
        Area = new(this, "area", mask, size, enabled: true, onBodyEnter: OnEnter);
        RegisterComponent(Area);
    }
    private void OnEnter(SwEntity entity)
    {
        var command = Props.Get("fields/on_enter_json");
        if(command is PriNull) return;
        if(Props.TryGet("is_command_global", out bool b) && b) SwApp.CommandStore.AddCommand(command);
        else entity.AddCommand(command);
        if(!Props.TryGet("max_activations", out int max_activations)) max_activations = 1;
        if(max_activations < 0) return;
        Activations++;
        if(Activations >= max_activations) Area.Enabled = false;
    }
}
