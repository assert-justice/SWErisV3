using SpoonWitch.ByteStream;

namespace SpoonWitch.Game.Entity.Component;

public abstract class SwComponent
{
    public readonly SwEntity Parent;
    public readonly string Name;
    public SwComponent(SwEntity parent, string name)
    {
        Parent = parent;
        Name = name;
    }
    public virtual void Read(SwByteStream byteStream)
    {
        //
    }
    public virtual void Write(SwByteStream byteStream)
    {
        //
    }
    public virtual void Update()
    {
        //
    }
    public virtual void Draw(SwComponent nextState)
    {
        //
    }
}