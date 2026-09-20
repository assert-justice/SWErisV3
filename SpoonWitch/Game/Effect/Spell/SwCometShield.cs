using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Entity;
using SpoonWitch.Game.Entity.Actor;
using SpoonWitch.Game.Entity.Projectile;

namespace SpoonWitch.Game.Effect.Spell;

public class SwCometShield(SwActor parent): SwSpell
{
    private readonly List<SwProjectile> Projectiles = [];
    private readonly Queue<SwProjectile> RemoveQueue = [];
    public readonly SwActor Parent = parent;
    public int NumProjectiles = 4;
    public PriNode ProjectileData = new PriDict();
    public double Radius = 32;
    public double Speed = 100;
    public override double ManaCost => 50;
    public override void Begin()
    {
        base.Begin();
        double angle = 0;
        for (int idx = 0; idx < NumProjectiles; idx++)
        {
            SwProjectile projectile = new();
            projectile.SetProps(ProjectileData.DeepCopy());
            SwGame.Game.AddEntity(projectile);
            Projectiles.Add(projectile);
            ErVec2 offset = ErVec2.FromAngle(angle) * Radius;
            projectile.Position = Parent.Position + offset;
            ErVec2 velocity = ErVec2.FromAngle(angle + ErMath.HALF_PI) * Speed;
            projectile.Velocity = velocity + Parent.Velocity;
            angle += ErMath.TAU / NumProjectiles;
        }
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
            var item = Projectiles[idx];
            if (item.IsFreeQueued)
            {
                RemoveQueue.Enqueue(item);
                continue;
            }
            var diff = item.Position - Parent.Position;
            var angle = diff.GetAngle();
            ErVec2 offset = ErVec2.FromAngle(angle) * Radius;
            item.Position = Parent.Position + offset;
            ErVec2 velocity = ErVec2.FromAngle(angle + ErMath.HALF_PI) * Speed;
            item.Velocity = velocity + Parent.Velocity;
        }
        while(RemoveQueue.TryDequeue(out var item)) Projectiles.Remove(item);
        if(Projectiles.Count == 0) End();
    }
}
