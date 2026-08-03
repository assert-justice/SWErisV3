using Eris;
using ErisMath;
using SpoonWitch.ByteStream;
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
    public double TimeoutClock;
    private readonly SwStateMachine StateMachine;
    public override double BaseSpeed => 100;
    public double WanderSpeedMul = 0.5;

    public SwSlume()
    {
        string path = "game_data/entities/actors/slume/slume_anim_data.json";
        if(!TryLoadSprites(path)) ErEngine.LogWarning("failed to load slume sprites");
        StateMachine = SwSlumeState.GetStateMachine(this, "state_machine");
        RegisterComponent(StateMachine);
        StateMachine.SetState("wandering");
    }
    protected override void Die()
    {
        base.Die();
        QueueFree();
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
}