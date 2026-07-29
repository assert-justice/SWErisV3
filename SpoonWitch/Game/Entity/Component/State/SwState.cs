using SpoonWitch.ByteStream;

namespace SpoonWitch.Game.Entity.Component.State;

public abstract class SwState
{
    public abstract string Name{get;}
    public readonly SwEntity Parent;
    public SwState(SwEntity parent)
    {
        Parent = parent;
    }
    public virtual void BeginState(string lastState){}
    public virtual void EndState(string nextState){}
    public virtual void Update(){}
    public virtual void Draw(SwState state){}
    public virtual void Read(SwByteStream byteStream){}
    public virtual void Write(SwByteStream byteStream){}
}