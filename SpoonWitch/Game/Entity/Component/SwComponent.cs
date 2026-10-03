using SpoonWitch.ByteStream;

namespace SpoonWitch.Game.Entity.Component;

public abstract class SwComponent(SwEntity parent, string name)
{
    public readonly SwEntity Parent = parent;
    public readonly string Name = name;

    public virtual void Read(SwByteStream byteStream)
    {
        //
    }
    public virtual void Write(SwByteStream byteStream)
    {
        //
    }
    public virtual void Ready()
    {
        //
    }
    public virtual void Update(double dt)
    {
        //
    }
    public virtual void Draw()
    {
        //
    }
    public virtual void Cleanup()
    {
        //
    }
}
