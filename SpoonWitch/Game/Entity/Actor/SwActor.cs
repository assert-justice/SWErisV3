using ErisMath;
using SpoonWitch.ByteStream;

namespace SpoonWitch.Game.Entity.Actor;

public abstract class SwActor: SwEntity
{
    public virtual double Speed => 300;
    // public virtual ErVec2 Size{get => new(32,32);}
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
        // ErVec2 offset = new(16,16);
        // var pos = Position - offset;
        // SwGame.MoveAndSlide(0, 1, new(32,32), ref pos, ref Velocity);
        // Position = pos + offset;
    }
}