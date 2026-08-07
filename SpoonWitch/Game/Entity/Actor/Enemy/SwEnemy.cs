using Eris;
using ErisMath;
using SpoonWitch.ByteStream;

namespace SpoonWitch.Game.Entity.Actor.Enemy;

public abstract class SwEnemy: SwActor
{
    public override uint Mask => 5;
    public bool IsPassive;
    public ErVec2 TargetPosition;
    public byte FacingIdx;
    public override void Ready()
    {
        base.Ready();
        IsPassive = EntProps.Props.TryGet("is_passive", out bool isPassive) && isPassive;
        ErEngine.Log("is passive ", IsPassive);
    }
    public bool CanSeeTarget()
    {
        return !SwGame.GetMap().CollisionLayer.Raycast(2, Position, TargetPosition);
    }
    public bool CanSeePoint(ErVec2 point)
    {
        return !SwGame.GetMap().CollisionLayer.Raycast(2, Position, point);
    }
    public bool CanSeePlayer()
    {
        return CanSeePoint(SwGame.PlayerPos);
    }
    public void MoveToTarget(double speed)
    {
        var dir = TargetPosition - Position;
        Velocity = dir.Normalized() * speed;
    }
    public double DistanceToTarget()
    {
        return (TargetPosition - Position).GetLength();
    }
    public override void Update()
    {
        base.Update();
        if(Velocity.IsNonzero()) FacingIdx = (byte)ErMath.RoundAngleToInt(Velocity.GetAngle(), 4);
    }
    public override void Read(SwByteStream byteStream)
    {
        base.Read(byteStream);
        if(!byteStream.TryReadVec2(out TargetPosition)) ErEngine.LogWarning("bad target pos");
        if(!byteStream.TryReadBool(out IsPassive)) ErEngine.LogWarning("bad is_passive");
        if(!byteStream.TryReadByte(out FacingIdx)) ErEngine.LogWarning("bad facing");
    }
    public override void Write(SwByteStream byteStream)
    {
        base.Write(byteStream);
        byteStream.WriteVec2(TargetPosition);
        byteStream.WriteBool(IsPassive);
        byteStream.WriteByte(FacingIdx);
    }
}