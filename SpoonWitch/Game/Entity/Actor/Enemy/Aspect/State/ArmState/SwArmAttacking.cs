using ErisMath;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State.ArmState;

public class SwArmAttacking: SwAspectArmState
{
    public override string Name => "attacking";
    private ErVec2 GetTarget()
    {
        foreach (var item in StateM.VisionArea.Area.OverlappingBodies)
        {
            return item.Position;
        }
        return Arm.TipPos;
    }
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        Arm.SetTarget(GetTarget());
        Arm.TipSpeed = Entity.BaseArmSpeed * 2;
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        if(!Arm.IsAtTarget) return;
        StateM.SetState("cooldown");
    }
}
