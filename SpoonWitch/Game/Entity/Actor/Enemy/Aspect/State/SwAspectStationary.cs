using ErisMath;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State;

public class SwAspectStationary : SwAspectState
{
    public override string Name => "stationary";
    private ErVec2 RightArmTarget;
    private ErVec2 RightArmTip;
    private ErVec2 LeftArmTarget;
    private ErVec2 LeftArmTip;
    private readonly double ArmSpeed = 100;
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        BodySprite.Play("phase1_idle");
        // RightArmTarget = GetRandomPos();
    }
    private ErVec2 GetRandomPos()
    {
        double angle = Random.Shared.NextDouble();
        return ErVec2.FromAngle(angle * ErMath.TAU) * 64;
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        ErVec2 diff = RightArmTarget - RightArmTip;
        if(diff.IsNonzero())
        {
            var len = diff.GetLength();
            if(len<ArmSpeed*dt) RightArmTip = RightArmTarget;
            else
            {
                RightArmTip += diff.Normalized() * dt * ArmSpeed;
                RightArm.SetTarget(RightArmTip);
            }
        }
        else
        {
            RightArmTarget = GetRandomPos() + new ErVec2(48,-32);
        }
        diff = LeftArmTarget - LeftArmTip;
        if(diff.IsNonzero())
        {
            var len = diff.GetLength();
            if(len<ArmSpeed*dt) LeftArmTip = LeftArmTarget;
            else
            {
                LeftArmTip += diff.Normalized() * dt * ArmSpeed;
                LeftArm.SetTarget(LeftArmTip);
            }
        }
        else
        {
            LeftArmTarget = GetRandomPos() + new ErVec2(-48,-32);
        }
    }
}
