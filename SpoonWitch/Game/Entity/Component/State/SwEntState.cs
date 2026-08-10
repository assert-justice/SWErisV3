namespace SpoonWitch.Game.Entity.Component.State;

public abstract class SwEntState<T>: SwState where T: SwEntity
{
    protected T Entity{get; private set;} = null!;
    public override void Init(SwStateMachine stateMachine)
    {
        base.Init(stateMachine);
        if(stateMachine.Parent is not T ent) throw new("bad parent type");
        Entity = ent;
    }
}