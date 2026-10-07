namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State;

public class SwAspectWake : SwAspectState
{
    public override string Name => "wake";
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        BodySprite.Play("wake");
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        if(!BodySprite.IsPlaying) StateMachine.SetState("uproot");
    }
}
