using Eris;
using ErisMath;
using SpoonWitch.ByteStream;

namespace SpoonWitch.Game.Entity.Actor.Enemy;

public abstract class SwEnemy: SwActor
{
    public override uint Mask => (uint)(IsAlive ? 5 : 0);
    public bool IsPassive;
    public ErVec2 TargetPosition;
    public byte FacingIdx;
    public override void Ready()
    {
        base.Ready();
        IsPassive = EntProps.Props.TryGet("property_overrides_json/is_passive", out bool isPassive) && isPassive;
    }
    public bool CanSeePoint(ErVec2 point)
    {
        if(SwApp.Debug) return !SwGame.Map.PhysicsWorld.RaycastDebug(2, Position, point);
        else return !SwGame.Map.PhysicsWorld.Raycast(2, Position, point);
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