namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State;

public class SwAspectUproot : SwAspectState
{
    public override string Name => "uproot";
        public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        BodySprite.Play("transition_1");
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        if(!BodySprite.IsPlaying) StateMachine.SetState("wandering");
    }
}
