using Eris;
using Prion.Node;
using SpoonWitch.ByteStream;
using SpoonWitch.Command;
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
    public override uint Mask => 3;
    public double ChargeTime => 1;
    public double ChargeSpeedMul => 0.5;
    // Note: dodge animations run at 12 fps, so 3/12 is 0.25 seconds
    public double DodgeInvulnDelay => 3.0 / 12;
    public double DodgeInvulnWindow => 4.0 / 12;
    public double DodgeDuration => 9.0 / 12;
    public double DodgeCooldown => 0.15;
    public double DodgeSpeedMul => 1.5;
    protected override int NumClocks => base.NumClocks + 2;
    public double Clock0{get => Clocks[base.NumClocks+0]; set {Clocks[base.NumClocks+0] = value;}}
    public double DodgeCooldownClock{get => Clocks[base.NumClocks+1]; set {Clocks[base.NumClocks+1] = value;}}
    private readonly SwStateMachine StateMachine;
    private readonly SwPlayerControls Controls;
    public SwPlayer()
    {
        Controls = new SwPlayerControls(this);
        RegisterComponent(Controls);
        string path = "game_data/entities/actors/player/player_anim_data.json";
        if(!TryLoadSprites(path)) ErEngine.LogWarning("failed to load player sprites");
        StateMachine = SwPlayerState.GetStateMachine(this, "state_machine");
        RegisterComponent(StateMachine);
    }
    private static void SetHud(string key, double value)
    {
        PriDict dict = new();
        dict.TrySet("verb", "hud_set");
        dict.TrySet("key", key);
        dict.TrySet("value", value);
        // dict.Data["verb"] = 
        // dict.Data["key"] = new PriString(key);
        // dict.Data["value"] = new PriNumber(value);
        SwApp.CommandStore.AddGlobalCommand(dict);
    }
    public override void Ready()
    {
        base.Ready();
    }
    public override void Update()
    {
        base.Update();
        if(DodgeCooldownClock > 0) DodgeCooldownClock -= SwGame.DeltaTime;
        SwGame.SetPlayerPos(Position);
    }
    protected override double Damage(PriNode command)
    {
        double value = base.Damage(command);
        if(value > 0)
        {
            SetHud("health", Health);
        }
        return value;
    }
}