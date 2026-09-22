using SpoonWitch.ByteStream;

namespace SpoonWitch.Game.Entity.Component.State;

public abstract class SwState
{
    public abstract string Name{get;}
    protected SwStateMachine StateMachine{get; private set;} = null!;
    public void Init(SwStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }
    public virtual void Ready(){}
    public virtual void BeginState(string lastState){}
    public virtual void EndState(string nextState){}
    public virtual void Update(){}
    public virtual void Draw(SwState state){}
    public virtual void Read(SwByteStream byteStream){}
    public virtual void Write(SwByteStream byteStream){}
}