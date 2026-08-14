using Eris;
using Prion.Node;
using SpoonWitch.ByteStream;
using SpoonWitch.Command;
using SpoonWitch.Game.Entity.Component.State;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Knight;

public class SwKnight : SwEnemy, ISwEntity<SwKnight>
{
    //
    public static byte TypeId => 2;
    private static SwKnight? _Primary;
    private static SwKnight? _Secondary;
    public static SwKnight Primary => _Primary ??= new();
    public static SwKnight Secondary => _Secondary ??= new();
    protected override byte GetTypeId => TypeId;
    private readonly SwStateMachine StateMachine;
    public double WanderSpeedMul = 0.25;
    public double TimeoutClock;
    public SwKnight()
    {
        string path = "game_data/entities/actors/knight/knight_anim_data.json";
        if(!TryLoadSprites(path)) ErEngine.LogWarning("failed to load knight sprites");
        StateMachine = SwKnightState.GetStateMachine(this, "state_machine");
        RegisterComponent(StateMachine);
    }
    public override void Ready()
    {
        base.Ready();
        if(!IsPassive) StateMachine.SetState("wandering");
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
    protected override void Die()
    {
        base.Die();
        StateMachine.SetState("dead");
    }
    protected override double Damage(SwDamage damage)
    {
        double value = base.Damage(damage);
        if(value > 0) StateMachine.SetState("knockback");
        return value;
    }
}