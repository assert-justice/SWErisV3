using ErisMath;
using SpoonWitch.Ik;

namespace SpoonWitch.Game.Entity.Component;

public class SwIkLimbComponent : SwComponent
{
    public SwIkLimb Limb{get; private set;} = new();
    public ErVec2 Offset;
    public SwIkLimbComponent(SwEntity parent, string name) : base(parent, name)
    {
    }
    public SwIkLimbComponent(SwEntity parent, string name, SwIkLimb limb) : base(parent, name)
    {
        Limb = limb;
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        Limb.Origin = Parent.Position + Offset;
        Limb.Update(dt);
    }
    public override void Draw()
    {
        base.Draw();
        Limb.DebugDraw();
    }
}
