using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Effect;
using SpoonWitch.Game.Map.Collision;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Actor;

public abstract class SwActor: SwEntity
{
    public double BaseSpeed = 150;
    public double MaxHealth = 100;
    public double InvulnTime = 0.5;
    public double InvulnClock = 0;
    public virtual bool IsInvuln => InvulnClock > 0;
    public double KnockbackFactor = 1;
    public double KnockbackTime = 0.5;
    public double KnockbackClock = 0;
    public virtual bool IsKnockback => KnockbackClock > 0;
    public double FlickerTime = 0.5;
    private double FlickerClock = 0;
    public double FlickerLen = 1.0/8;
    private double FlickerCycle = 0;
    public double Health;
    private bool _IsAlive = true;
    public bool IsAlive
    {
        get => _IsAlive; 
        set => _IsAlive = value;
    }
    public ErVec2 Velocity;
    public ErVec2 Size = new (32, 32);
    public virtual uint Mask => 0;
    private readonly SwColliderBody Body;
    public SwActor()
    {
        AddHandler("damage", DamageHandler);
        Body = new();
    }
    protected override void SetProps(PriNode props)
    {
        base.SetProps(props);
        if(SwPrion.TryGetVec2(out var size, Props.Get("size"))) Size = size;
    }
    public override void Ready()
    {
        base.Ready();
        Health = MaxHealth;
        _IsAlive = true;
        Body.ParentId = Id;
    }
    private void HandleFlicker()
    {
        if(FlickerClock <= 0) return;
        FlickerClock -= SwGame.DeltaTime;
        if(FlickerClock <= 0)
        {
            Visible = true;
            return;
        }
        FlickerCycle -= SwGame.DeltaTime;
        if(FlickerCycle <= 0) FlickerCycle = FlickerLen;
        Visible = FlickerCycle > FlickerLen * 0.5;
    }
    public override void Update()
    {
        base.Update();
        if(InvulnClock > 0)InvulnClock -= SwGame.DeltaTime;
        if(KnockbackClock > 0)KnockbackClock -= SwGame.DeltaTime;
        HandleFlicker();
        Body.Mask = Mask;
        Body.Rect = ErRect2.Centered(Position, Size);
        Body.Velocity = Velocity;
        SwGame.Map.PhysicsWorld.MoveAndSlide(SwGame.DeltaTime, Id, Body);
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
        KnockbackClock = KnockbackTime;
        InvulnClock = InvulnTime;
        FlickerClock = FlickerTime;
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
    public double MoveToward(ErVec2 point, double speed)
    {
        var diff = point - Position;
        var distance = diff.GetLength();
        double spd = speed * SwGame.DeltaTime;
        if(distance < spd)
        {
            Position = point;
            distance = 0;
            Velocity = ErVec2.Zero;
        }
        else Velocity = diff.Normalized() * speed;
        return distance;
    }
}