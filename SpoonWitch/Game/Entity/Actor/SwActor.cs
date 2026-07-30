using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.ByteStream;
using SpoonWitch.Command;

namespace SpoonWitch.Game.Entity.Actor;

public abstract class SwActor: SwEntity
{
    public virtual double BaseSpeed => 300;
    public virtual double MaxHealth => 100;
    public virtual double InvulnTime => 0.5;
    public virtual bool IsInvuln => InvulnClock > 0;
    private double InvulnClock = 0;
    public double Health;
    private bool _IsAlive = true;
    public bool IsAlive => _IsAlive;
    public override void Ready()
    {
        base.Ready();
        // CommandHandler.AddHandler((c) =>{Damage(c);}, "damage", Id);
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
    public override void Update()
    {
        base.Update();
        SwGame.AddCollider(new(){ Id=Id,Mask=Mask,Rect=ErRect2.Centered(Position,Size)});
        if(InvulnClock > 0)InvulnClock -= SwGame.DeltaTime;
    }
    private static bool TryParseDamage(PriNode node, out double value)
    {
        if(node.TryAs(out value)) return true;
        if(node.Get("value").TryAs(out value)) return true;
        return false;
    }
    protected override void HandleCommands()
    {
        base.HandleCommands();
        foreach (var item in SwApp.CommandStore.GetCommands("damage", Id))
        {
            Damage(item);
        }
    }
    protected virtual double Damage(SwCommand command)
    {
        if(!TryParseDamage(command.Payload, out double value))
        {
            ErEngine.LogWarning("no damage value");
            return 0;
        }
        if(IsInvuln) return 0;
        if(!IsAlive) return 0;
        Health -= value;
        InvulnClock = InvulnTime;
        if(Health > 0)
        {
            ErEngine.Log("entity ", Id," '", GetType(), "' took ", value, " damage. health is now ", Health);
        }
        else
        {
            ErEngine.Log("entity ", Id," '", GetType(), "' took ", value, " damage and died.");
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