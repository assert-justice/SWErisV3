using ErisMath;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State;

public class SwAspectStationary : SwAspectState
{
    public override string Name => "stationary";
    private readonly double ArmSpeed = 100;
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        BodySprite.Play("phase1_idle");
        LeftArm.TipSpeed = ArmSpeed;
        RightArm.TipSpeed = ArmSpeed;
    }
    private ErVec2 GetRandomPointInCircle(ErVec2 center, double radius)
    {
        double angle = Random.Shared.NextDouble();
        return ErVec2.FromAngle(angle * ErMath.TAU) * radius + center;
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        if(LeftArm.IsAtTarget) LeftArm.Target = GetRandomPointInCircle(new ErVec2(-64,-32),48);
        if(RightArm.IsAtTarget) RightArm.Target = GetRandomPointInCircle(new ErVec2(64,-32),48);
    }
}
