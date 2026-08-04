using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.ByteStream;
using SpoonWitch.Command;

namespace SpoonWitch.Game.Entity.Actor;

public abstract class SwActor: SwEntity
{
    public virtual double BaseSpeed => 150;
    public virtual double MaxHealth => 100;
    public virtual double InvulnTime => 0.5;
    private double InvulnClock = 0;
    public virtual bool IsInvuln => InvulnClock > 0;
    public virtual double KnockbackFactor => 10;
    public virtual double KnockbackTime => 0.5;
    private double KnockbackClock = 0;
    public virtual bool IsKnockback => KnockbackClock > 0;
    public virtual double FlickerTime => 0.5;
    private double FlickerClock = 0;
    public virtual double FlickerLen => 1.0/8;
    private double FlickerCycle = 0;
    public double Health;
    private bool _IsAlive = true;
    public bool IsAlive => _IsAlive;
    public override void Ready()
    {
        base.Ready();
        Health = MaxHealth;
        _IsAlive = true;
    }
    public override void Read(SwByteStream byteStream)
    {
        base.Read(byteStream);
        if(!byteStream.TryReadF64(out Health)) throw new("no health");
        if(!byteStream.TryReadF64(out InvulnClock)) throw new("no invuln clock");
        if(!byteStream.TryReadBool(out _IsAlive))  throw new("no is alive clock");
    }
    public override void Write(SwByteStream byteStream)
    {
        base.Write(byteStream);
        byteStream.WriteF64(Health);
        byteStream.WriteF64(InvulnClock);
        byteStream.WriteBool(_IsAlive);
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
        if(IsAlive) SwGame.AddCollider(new(){ Id=Id,Mask=Mask,Rect=ErRect2.Centered(Position,Size)});
        if(InvulnClock > 0)InvulnClock -= SwGame.DeltaTime;
        if(KnockbackClock > 0)KnockbackClock -= SwGame.DeltaTime;
        HandleFlicker();
        // if(FlickerClock > 0)
        // {
        //     FlickerClock -= SwGame.DeltaTime;
        //     if(FlickerClock <= 0) Visible = true;
        // }
    }
    // private static bool TryParseDamage(PriNode node, out double value)
    // {
    //     if(node.TryAs(out value)) return true;
    //     if(node.Get("value").TryAs(out value)) return true;
    //     return false;
    // }
    protected override void HandleCommands()
    {
        base.HandleCommands();
        // foreach (var item in SwApp.CommandStore.GetCommands("damage", Id))
        foreach (var item in EntProps.GetCommands())
        {
            Damage(item);
        }
    }
    private string GetTypeName()
    {
        return GetType().ToString().Split('.')[^1];
    }
    protected virtual double Damage(SwCommand command)
    {
        if(!SwDamage.TryFromPri(command.Payload, out var damage))
        // if(!TryParseDamage(command.Payload, out double value))
        {
            ErEngine.LogWarning("no damage value");
            return 0;
        }
        if(IsInvuln) return 0;
        if(!IsAlive) return 0;
        double value = damage.Value;
        var knockback = (Position - damage.SourcePos).Normalized() * value * KnockbackFactor;
        Velocity = knockback;
        Health -= value;
        KnockbackClock = KnockbackTime;
        InvulnClock = InvulnTime;
        FlickerClock = FlickerTime;
        if(Health > 0)
        {
            ErEngine.Log("entity ", Id," '", GetTypeName(), "' took ", value, " damage. health is now ", Health);
        }
        else
        {
            ErEngine.Log("entity ", Id," '", GetTypeName(), "' took ", value, " damage and died.");
            Die();
            _IsAlive = false;
        }
        return value;
    }
    protected virtual void Die()
    {
        if(!IsAlive) throw new("tried to die twice. should be unreachable");
        ErEngine.Log("entity ", Id," died.");
    }
}