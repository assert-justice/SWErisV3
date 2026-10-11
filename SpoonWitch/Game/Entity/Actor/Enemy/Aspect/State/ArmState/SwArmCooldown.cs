namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State.ArmState;

public class SwArmCooldown: SwAspectArmState
{
    public override string Name => "cooldown";
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        Clock.Start(2);
        Arm.SetTarget(Entity.Position);
        Arm.TipSpeed = StateM.Tracking ? Entity.BaseArmSpeed : Entity.BaseArmSpeed /3;
        StateM.VisionArea.Enabled = false;
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        Arm.SetTarget(Entity.Position);
        if(!Clock.IsRunning) StateM.SetState("awake");
    }
}
