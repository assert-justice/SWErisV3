using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.ByteStream;

namespace SpoonWitch.Game.Entity.Actor.Enemy;

public abstract class SwEnemy: SwActor
{
    public override uint Mask => (uint)(IsAlive ? 5 : 0);
    public override int RenderLayer => 2;
    public bool IsPassive;
    public ErVec2 TargetPosition;
    public byte FacingIdx;
    protected override void SetProps(PriNode props)
    {
        base.SetProps(props);
        IsPassive = Props.TryGet("is_passive", out bool isPassive) && isPassive;
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
}
