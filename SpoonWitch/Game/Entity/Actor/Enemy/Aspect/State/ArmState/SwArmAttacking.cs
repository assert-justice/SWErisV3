using ErisMath;
using ErisPhysics2D.Collider;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State.ArmState;

public class SwArmAttacking: SwAspectArmState
{
    public override string Name => "attacking";
    ErColliderBody? TargetBody;
    private ErVec2 GetTarget()
    {
        foreach (var item in StateM.VisionArea.Area.OverlappingBodies)
        {
            if(StateM.Tracking) TargetBody = item;
            return item.Position;
        }
        return Arm.TipPos;
    }
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        Arm.SetTarget(GetTarget());
        Arm.TipSpeed = Entity.BaseArmSpeed * 3;
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        if(TargetBody is not null) Arm.SetTarget(TargetBody.Position);
        if(Arm.IsAtTarget || Arm.TargetOutOfReach)
        {
            StateM.SetState("cooldown");
            TargetBody = null;
        }
    }
}
