using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Effect;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Component;

public class SwHurtboxComponent : SwAreaComponent
{
    public PriNode DamageCommand = PriNull.Null;
    public SwHurtboxComponent(SwEntity parent, string name) : base(parent, name, 0, new ErVec2(32,32))
    {
        OnBodyEnter = OnEnter;
    }
    public static bool TryFromData(out SwHurtboxComponent hurtboxComponent, SwEntity parent, PriNode data)
    {
        if(!data.TryGet("name", out string name)) name = "hurtbox";
        hurtboxComponent = new(parent, name);
        if(SwPrion.TryGetVec2(out var size, data.Get("size"))) hurtboxComponent.Size = size; 
        if(data.TryGet("mask", out uint u)) hurtboxComponent.Mask = u;
        if(data.TryGet("is_enabled", out bool b)) hurtboxComponent.Enabled = b;
        if(!SwDamage.TryFromPri(data.Get("damage"), out var _)) return ErEngine.LogWarning("hurtbox failed to parse damage");
        return true;
    }
    private void OnEnter(SwEntity entity)
    {
        entity.AddCommand(DamageCommand);
    }
}
