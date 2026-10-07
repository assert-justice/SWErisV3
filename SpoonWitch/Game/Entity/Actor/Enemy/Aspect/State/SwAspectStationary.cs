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
        LeftArm.Activate();
        RightArm.TipSpeed = Entity.BaseArmSpeed;
        RightArm.Activate();
        Legs.Activate();
        Legs.Randomize();
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        ErVec2 target;
        if (LeftArm.IsAtTarget)
        {
            target = Entity.Position + SwRandom.GetRandomPointOnCircle(new ErVec2(-64,-32),48);
            LeftArm.SetTarget(target);
        }
        if (RightArm.IsAtTarget)
        {
            target = Entity.Position + SwRandom.GetRandomPointOnCircle(new ErVec2(64,-32),48);
            RightArm.SetTarget(target);
        }
    }
}
