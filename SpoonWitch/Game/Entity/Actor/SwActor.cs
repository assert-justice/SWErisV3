using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Effect;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Actor;

public abstract class SwActor: SwEntity
{
    public double BaseSpeed = 150;
    public double MaxHealth = 100;
    public double InvulnTime = 0.5;
    public readonly SwClock InvulnClock;
    public virtual bool IsInvuln => InvulnClock.IsRunning;
    public double KnockbackFactor = 1;
    public double KnockbackTime = 0.5;
    public readonly SwClock KnockbackClock;
    public virtual bool IsKnockback => KnockbackClock.IsRunning;
    private readonly SwClock FlickerClock;
    public double FlickerLen = 1.0/16;
    private readonly SwClock FlickerCycle;
    public double Health;
    private bool _IsAlive = true;
    public bool IsAlive
    {
        get => _IsAlive; 
        set => _IsAlive = value;
    }
    public ErVec2 Velocity;
    public ErVec2 Size = new (32, 32);
    public virtual SwCollisionMask Mask => 0;
    private SwColliderBody Body = null!;
    public SwActor()
    {
        InvulnClock = AddClock();
        KnockbackClock = AddClock();
        FlickerClock = AddClock();
        FlickerClock.OnFinish = OnFlickerFinish;
        FlickerCycle = AddClock(FlickerLen);
        FlickerCycle.OnFinish = OnFlickerCycle;
        AddHandler("damage", DamageHandler);
    }
    public override void Init()
    {
        base.Init();
        Body = new(SwApp.GetNextId(), Id);
        _IsAlive = true;
    }
    public override void SetProps(PriNode props)
    {
        base.SetProps(props);
        if(SwPrion.TryGetVec2(out var size, Props.Get("size"))) Size = size;
        if(Props.TryGet("health/max_health", out double d)) MaxHealth = d; 
        if(Props.TryGet("health/health", out d)) Health = d; 
        if(Props.TryGet("speed/base_speed", out d)) BaseSpeed = d;
    }
    public override void Ready()
    {
        base.Ready();
    }
    private void OnFlickerFinish()
    {
        FlickerCycle.Pause();
        Visible = true;
    }
    private void OnFlickerCycle()
    {
        Visible = !Visible;
        FlickerCycle.Restart();
    }
    protected override void Update(double dt)
    {
        base.Update(dt);
        Body.Mask = (uint)Mask;
        Body.Rect = ErRect2.Centered(Position, Size);
        Body.Velocity = Velocity;
        ErVec2 vel = Velocity;
        Game.PhysicsWorld.MoveAndSlide(dt, Body);
        Position = Body.Rect.Center;
        Velocity = Body.Velocity;
    }
    private string GetTypeName()
    {
        return GetType().ToString().Split('.')[^1];
    }
    private void DamageHandler(PriNode command)
    {
        if(!SwDamage.TryFromPri(command, out var damage))
        {
            ErEngine.LogWarning("bad damage");
            return;
        }
        Damage(damage);
    }
    protected virtual double Damage(SwDamage damage)
    {
        if(IsInvuln) return 0;
        if(!IsAlive) return 0;
        double value = 0;
        foreach (var item in damage.Entries)
        {
            value += item.Item2;
        }
        var knockback = (Position - damage.SourcePos).Normalized() * value * KnockbackFactor;
        Velocity = knockback;
        Health -= value;
        KnockbackClock.Start(KnockbackTime);
        if(InvulnTime > 0)
        {
            InvulnClock.Start(InvulnTime);
            FlickerClock.Start(InvulnTime);
            FlickerCycle.Start();
        }
        if(Health > 0)
        {
            if(SwApp.Debug) ErEngine.Log("entity ", Id," '", GetTypeName(), "' took ", value, " damage. health is now ", Health);
        }
        else
        {
            if(SwApp.Debug) ErEngine.Log("entity ", Id," '", GetTypeName(), "' took ", value, " damage and died.");
            Die();
        }
        return value;
    }
    protected virtual void Die()
    {
        if(!IsAlive) throw new("tried to die twice. should be unreachable");
        IsAlive = false;
        if(SwApp.Debug) ErEngine.Log("entity ", Id," died.");
    }
    // Todo: figure out how I want to implement this
    // public double MoveToward(ErVec2 point, double speed)
    // {
    //     var diff = point - Position;
    //     var distance = diff.GetLength();
    //     // Note: the tickrate 
    //     double spd = speed / ErEngine.Tickrate;
    //     if(distance < spd)
    //     {
    //         Position = point;
    //         distance = 0;
    //         Velocity = ErVec2.Zero;
    //     }
    //     else Velocity = diff.Normalized() * speed;
    //     return distance;
    // }
    public void MoveToward(ErVec2 point, double speed)
    {
        Velocity = (point - Position).Normalized() * speed;
    }
    public double MoveToward(ErVec2 point, double speed, double dt)
    {
        var (dir,len) = (point - Position).GetDirLen();
        if(len < speed * dt)
        {
            Position = point;
            Velocity = ErVec2.Zero;
            return 0;
        }
        Velocity = dir * speed;
        return len;
    }
    public void SetInvulnerable(double duration)
    {
        InvulnClock.Start(duration);
    }
    public override void GameCleanup()
    {
        base.GameCleanup();
        Game.PhysicsWorld.RemoveBody(Body.Id);
    }
}