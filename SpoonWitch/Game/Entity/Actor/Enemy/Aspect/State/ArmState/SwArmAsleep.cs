namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State.ArmState;

public class SwArmAsleep: SwAspectArmState
{
    public override string Name => "asleep";
    public override void EndState(string nextState)
    {
        base.EndState(nextState);
        Arm.Activate();
        StateM.Hurtbox.Enabled = true;
    }
}
