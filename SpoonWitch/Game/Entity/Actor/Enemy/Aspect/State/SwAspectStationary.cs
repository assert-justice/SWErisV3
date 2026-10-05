using ErisMath;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State;

public class SwAspectStationary : SwAspectState
{
    public override string Name => "stationary";
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        BodySprite.Play("phase1_idle");
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        if (RightArm.IsAtTarget)
        {
            double angle = Random.Shared.NextDouble();
            RightArm.Target = ErVec2.FromAngle(angle * ErMath.TAU) * 64;
        }
    }
}
