using Eris;
using ErisMath;
using SpoonWitch.ByteStream;
using SpoonWitch.Command;

namespace SpoonWitch.Game.Entity.Actor;

public abstract class SwActor: SwEntity
{
    public virtual double BaseSpeed => 300;
    public override void Ready()
    {
        base.Ready();
        CommandHandler.AddHandler(Damage, "damage", Id);
    }
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
    protected virtual void Damage(SwCommand command)
    {
        ErEngine.Log("entity ", Id," '", GetType(), "' took damage");
    }
}