using Eris;
using Eris.Renderer;
using ErisMath;
using SpoonWitch.ByteStream;

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
    public SwPlayer()
    {
        string path = "game_data/entities/actors/player/player_anim_data.json";
        if(!TryLoadSprites(path)) ErEngine.LogWarning("failed to load player sprites");
        RegisterComponent(SwPlayerState.GetPlayerStateMachine(this, "state_machine"));
        Position = new(128,128);
    }
    // public override void Read(SwByteStream byteStream)
    // {
    //     base.Read(byteStream);
    // }
    // public override void Write(SwByteStream byteStream)
    // {
    //     base.Write(byteStream);
    // }
    public override void Update()
    {
        base.Update();
        SwGame.SetPlayerPos(Position);
    }
}