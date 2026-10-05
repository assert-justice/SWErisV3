using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.ByteStream;
using SpoonWitch.Game.Entity.Actor.Player;

namespace SpoonWitch.Game.Entity.Actor.Enemy;

public abstract class SwEnemy: SwActor
{
    public override SwCollisionMask Mask => IsAlive ? SwCollisionMask.Enemy : SwCollisionMask.None;
    public override int RenderLayer => 2;
    public bool IsPassive;
    public ErVec2 TargetPosition;
    public byte FacingIdx;
    public SwEnemy()
    {
        AddGlobalHandler("get_mad", (_)=>GetMad());
    }
    public override void SetProps(PriNode props)
    {
        base.SetProps(props);
        IsPassive = Props.TryGet("is_passive", out bool isPassive) && isPassive;
    }
    public bool CanSeePoint(ErVec2 point)
    {
        return Game.PhysicsWorld.Raycast((uint)SwCollisionMask.IsOpaque, Position, point);
        // return false;
        // if(SwApp.Debug) return !SwGame.Map.PhysicsWorld.RaycastDebug(2, Position, point);
        // else return !SwGame.Map.PhysicsWorld.Raycast(2, Position, point);
    }
    public bool CanSeePlayer()
    {
        // return CanSeePoint(SwGame.PlayerPos);
        return false;
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
    public IEnumerable<T> GetVisibleEntities<T>() where T: SwEntity
    {
        foreach (var entity in Game.EntityLookup.GetValues<T>())
        {
            if(CanSeePoint(entity.Position)) yield return entity;
        }
    }
    public bool TryGetClosestEntity<T>(out T entity) where T: SwEntity
    {
        entity = null!;
        double minSqDis = double.MaxValue;
        foreach (var ent in GetVisibleEntities<T>())
        {
            double sqDis = (Position - ent.Position).GetLengthSquared();
            if(sqDis < minSqDis)
            {
                minSqDis = sqDis;
                entity = ent;
            }
        }
        return entity is not null;
    }
    protected override void Update(double dt)
    {
        base.Update(dt);
        if(Velocity.IsNonzero()) FacingIdx = (byte)ErMath.RoundAngleToInt(Velocity.GetAngle(), 4);
    }
    public virtual void GetMad()
    {
        IsPassive = false;
    }
}
