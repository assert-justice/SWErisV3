using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.MapEntity;

public class SwTrigger : SwMapEntity
{
    private SwAreaComponent Area = null!;
    public SwCollisionMask Mask;
    public int Activations
    {
        get => Props.TryGet("activations", out int activations) ? activations : 0;
        set => Props.TrySet("activations", value);
    }
    public SwTrigger()
    {
    }
    public override void SetProps(PriNode props)
    {
        base.SetProps(props);
        if(Props.TryGet("mask", out uint mask)) Mask = (SwCollisionMask)mask;
        if(Area is not null)
        {
            Area.Mask = mask;
            Area.Size = Size;
        }
    }
    public override void Init()
    {
        base.Init();
        Area = new(this, "area", (uint)Mask, Size, enabled: IsEnabled(), onBodyEnter: OnEnter);
        RegisterComponent(Area);
    }
    private bool IsEnabled()
    {
        if(!Props.TryGet("max_activations", out int max_activations)) max_activations = 1;
        if(max_activations < 0) return true;
        return Activations < max_activations;
    }
    private void OnEnter(SwEntity entity)
    {
        if (!IsEnabled())
        {
            ErEngine.LogWarning("attempted to activate trigger after it was disabled");
            return;
        }
        var command = Props.Get("fields/on_enter_json");
        if(command is PriNull) return;
        if(Props.TryGet("fields/is_command_global", out bool b) && b) SwApp.CommandQueue.AddCommand(command);
        else entity.AddCommand(command);
        Activations++;
        if(!IsEnabled()) Area.Enabled = false;
    }
}
