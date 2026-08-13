using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.ByteStream;
using SpoonWitch.Command;
using SpoonWitch.Game.Entity.Component.State;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Slume;

public class SwSlume : SwEnemy, ISwEntity<SwSlume>
{
    public static byte TypeId => 1;
    private static SwSlume? _Primary;
    private static SwSlume? _Secondary;
    public static SwSlume Primary => _Primary ??= new();
    public static SwSlume Secondary => _Secondary ??= new();
    protected override byte GetTypeId => TypeId;
    public override ErVec2 Size => new(16,16);
    public ErVec2 HurtboxSize = new(20, 20);
    public double TimeoutClock;
    private readonly SwStateMachine StateMachine;
    public override double BaseSpeed => 100;
    public double WanderSpeedMul = 0.5;
    public override double MaxHealth => 20;

    public SwSlume()
    {
        string path = "game_data/entities/actors/slume/slume_anim_data.json";
        if(!TryLoadSprites(path)) ErEngine.LogWarning("failed to load slume sprites");
        StateMachine = SwSlumeState.GetStateMachine(this, "state_machine");
        RegisterComponent(StateMachine);
        // StateMachine.SetState("wandering");
    }
    public override void Ready()
    {
        base.Ready();
        if(!IsPassive) StateMachine.SetState("wandering");
    }
    protected override void Die()
    {
        base.Die();
        // QueueFree();
        StateMachine.SetState("dead");
    }
    public override void Read(SwByteStream byteStream)
    {
        base.Read(byteStream);
        byteStream.TryReadF64(out TimeoutClock);
    }
    public override void Write(SwByteStream byteStream)
    {
        base.Write(byteStream);
        byteStream.WriteF64(TimeoutClock);
    }
    protected override double Damage(PriNode command)
    {
        double value = base.Damage(command);
        if(value > 0) StateMachine.SetState("knockback");
        return value;
    }
    public void DoDamage()
    {
        SwDamage damage = new(10, Position);
        // SwGame.EnqueueCommandRect(2, ErRect2.Centered(Position, HurtboxSize), damage.ToPri());
        // SwGame.EnqueueCommandRect(2, ErRect2.Centered(Position, HurtboxSize), new("damage", new PriNumber(10)));
    }
}