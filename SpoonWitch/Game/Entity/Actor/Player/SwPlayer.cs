using Eris;
using SpoonWitch.Game.Entity.Component.State;

namespace SpoonWitch.Game.Entity.Actor.Player;

public class SwPlayer: SwActor, ISwEntity<SwPlayer>
{
    private static SwPlayer? _Primary;
    private static SwPlayer? _Secondary;
    public static SwPlayer Primary => _Primary ??= new();
    public static SwPlayer Secondary => _Secondary ??= new();
    public static byte TypeId => 0;
    protected override byte GetTypeId => TypeId;
    // public override double Speed => StateMachine.CurrentState.Name switch
    // {
    //     "charging" or "charged" => ChargeSpeedMul * BaseSpeed,
    //     _ => BaseSpeed,
    // };
    public override uint Mask => 3;
    public double ChargeTime = 1;
    public double ChargeSpeedMul = 0.5;
    private readonly SwStateMachine StateMachine;
    public SwPlayer()
    {
        string path = "game_data/entities/actors/player/player_anim_data.json";
        if(!TryLoadSprites(path)) ErEngine.LogWarning("failed to load player sprites");
        StateMachine = SwPlayerState.GetPlayerStateMachine(this, "state_machine");
        RegisterComponent(StateMachine);
        Position = new(128,128);
    }
    public override void Update()
    {
        base.Update();
        SwGame.SetPlayerPos(Position);
    }
}