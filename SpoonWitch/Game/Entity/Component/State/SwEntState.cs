namespace SpoonWitch.Game.Entity.Component.State;

public abstract class SwEntState<T>: SwState where T: SwEntity
{
    protected T Entity{get; private set;} = null!;
    public override void Ready()
    {
        base.Ready();
        if(StateMachine.Parent is not T ent) throw new("bad parent type");
        Entity = ent;
    }
}
