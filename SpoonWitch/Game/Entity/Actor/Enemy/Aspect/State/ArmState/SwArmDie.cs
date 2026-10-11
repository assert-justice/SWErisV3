namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State.ArmState;

public class SwArmDie: SwAspectArmState
{
    public override string Name => "die";
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        Arm.SetTarget(Entity.Position);
        Arm.TipSpeed = Entity.BaseArmSpeed /8;
    }
}
