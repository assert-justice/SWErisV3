using SpoonWitch.ByteStream;

namespace SpoonWitch.Game.Entity.Component.State;

public abstract class SwState<T> where T: SwEntity
{
    public abstract string Name{get;}
    protected SwStateMachine<T> StateMachine{get; private set;} = null!;
    public T Entity => StateMachine.Entity;
    public void Init(SwStateMachine<T> stateMachine)
    {
        StateMachine = stateMachine;
    }
    public virtual void Ready(){}
    public virtual void BeginState(string lastState){}
    public virtual void EndState(string nextState){}
    public virtual void Update(double dt){}
    public virtual void Draw(){}
    public virtual void Read(SwByteStream byteStream){}
    public virtual void Write(SwByteStream byteStream){}
}