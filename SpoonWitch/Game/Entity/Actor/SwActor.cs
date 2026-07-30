using ErisMath;
using SpoonWitch.ByteStream;

namespace SpoonWitch.Game.Entity.Actor;

public abstract class SwActor: SwEntity
{
    public virtual double Speed => 300;
    public override void Read(SwByteStream byteStream)
    {
        base.Read(byteStream);
    }
    public override void Write(SwByteStream byteStream)
    {
        base.Write(byteStream);
    }
    public override void Update()
    {
        base.Update();
        SwGame.AddCollider(new(){ Id=Id,Mask=Mask,Rect=ErRect2.Centered(Position,Size)});
    }
}