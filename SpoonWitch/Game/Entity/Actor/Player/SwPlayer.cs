using Eris;
using Eris.Utils;
using SpoonWitch.Game.Entity.Component.Sprite;
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
    public double ChargeTime = 1;
    public double ChargeSpeedMul = 0.5;
    private readonly ErWrapper<SwSprite> ReticleSprite;// = new(()=>this.g)
    private readonly SwStateMachine StateMachine;
    private readonly SwPlayerControls Controls;
    public SwPlayer()
    {
        Controls = new SwPlayerControls(this);
        ReticleSprite = new(()=>GetComponent<SwSprite>("reticle")!);
        RegisterComponent(Controls);
        string path = "game_data/entities/actors/player/player_anim_data.json";
        if(!TryLoadSprites(path)) ErEngine.LogWarning("failed to load player sprites");
        StateMachine = SwPlayerState.GetStateMachine(this, "state_machine");
        RegisterComponent(StateMachine);
        Position = new(128,128);
    }
    public override void Update()
    {
        base.Update();

        // if(Controls.r)
        SwGame.SetPlayerPos(Position);
    }
}