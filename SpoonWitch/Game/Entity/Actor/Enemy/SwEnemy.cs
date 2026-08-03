using Eris;
using ErisMath;
using SpoonWitch.ByteStream;

namespace SpoonWitch.Game.Entity.Actor.Enemy;

public abstract class SwEnemy: SwActor
{
    public override uint Mask => 5;
    public ErVec2 TargetPosition;
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
        // return !SwGame.GetMap().CollisionLayer.Raycast(2, Position, point);
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
    public override void Read(SwByteStream byteStream)
    {
        base.Read(byteStream);
        if(!byteStream.TryReadVec2(out TargetPosition)) ErEngine.LogWarning("bad target pos");
    }
    public override void Write(SwByteStream byteStream)
    {
        base.Write(byteStream);
        byteStream.WriteVec2(TargetPosition);
    }
}