using Eris;
using ErisMath;
using SpoonWitch.Data;
using SpoonWitch.Game.Entity.Actor;
using SpoonWitch.Game.Entity.Projectile;

namespace SpoonWitch.Game.Effect.Spell;

public class SwCometShield(SwActor parent): SwSpell
{
    private readonly List<SwProjectile> Projectiles = [];
    private readonly Queue<SwProjectile> RemoveQueue = [];
    public readonly SwActor Parent = parent;
    public int NumProjectiles = 4;
    public double Radius = 32;
    public double Speed = 100;
    public override double ManaCost => 50;
    public override void Begin()
    {
        base.Begin();
        double angle = 0;
        for (int idx = 0; idx < NumProjectiles; idx++)
        {
            var props = SwData.Prototypes.Get("projectiles/comet").DeepCopy();
            var projectile = SwGame.Game.LoadEntity<SwProjectile>(props);
            Projectiles.Add(projectile);
            angle += ErMath.TAU / NumProjectiles;
            ProjectileHelp(projectile, angle);
        }
    }
    private void ProjectileHelp(SwProjectile projectile)
    {
        ProjectileHelp(projectile, (projectile.Position - Parent.Position).GetAngle());
    }
    private void ProjectileHelp(SwProjectile projectile, double angle)
    {
        ErVec2 offset = ErVec2.FromAngle(angle) * Radius;
        projectile.Position = Parent.Position + offset;
        ErVec2 velocity = ErVec2.FromAngle(angle + ErMath.HALF_PI) * Speed;
        projectile.Velocity = velocity + Parent.Velocity;
    }
    public override void End()
    {
        base.End();
        foreach (var item in Projectiles)
        {
            item.QueueFree();
        }
        Projectiles.Clear();
    }
    public override void Update()
    {
        base.Update();
        // remove dead projectiles
        // set all projectiles to be equidistant
        if(!IsActive) return;
        for (int idx  = 0; idx  < Projectiles.Count; idx ++)
        {
            var projectile = Projectiles[idx];
            if (projectile.IsFreeQueued)
            {
                RemoveQueue.Enqueue(projectile);
                continue;
            }
            ProjectileHelp(projectile);
        }
        while(RemoveQueue.TryDequeue(out var item)) Projectiles.Remove(item);
        if(Projectiles.Count == 0) End();
    }
}
