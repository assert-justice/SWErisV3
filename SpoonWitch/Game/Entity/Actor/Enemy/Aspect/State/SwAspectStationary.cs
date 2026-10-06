using ErisMath;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State;

public class SwAspectStationary : SwAspectState
{
    public override string Name => "stationary";
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        BodySprite.Play("phase1_idle");
        LeftArm.TipSpeed = Entity.BaseArmSpeed;
        LeftArm.IsActive = true;
        LeftArm.IsVisible = true;
        RightArm.TipSpeed = Entity.BaseArmSpeed;
        RightArm.IsActive = true;
        RightArm.IsVisible = true;
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        if(LeftArm.IsAtTarget) LeftArm.Target = SwRandom.GetRandomPointOnCircle(new ErVec2(-64,-32),48);
        if(RightArm.IsAtTarget) RightArm.Target = SwRandom.GetRandomPointOnCircle(new ErVec2(64,-32),48);
    }
}
