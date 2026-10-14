using ErisMath;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State;

public class SwAspectDead : SwAspectState
{
    public override string Name => "dead";
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        Entity.QueueFree();
        // Entity.Velocity = ErVec2.Zero;
        // Entity.TimeoutClock.Start()
    }
}
