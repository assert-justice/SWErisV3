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
    public SwPlayer()
    {
        string path = "game_data/entities/actors/player/player_anim_data.json";
        if(!TryLoadSprites(path)) ErEngine.LogWarning("failed to load player sprites");
        RegisterComponent(SwPlayerState.GetPlayerStateMachine(this, "state_machine"));
        Position = new(128,128);
    }
    public override void Read(SwByteStream byteStream)
    {
        base.Read(byteStream);
    }
    public override void Write(SwByteStream byteStream)
    {
        base.Write(byteStream);
    }
    public override void Update()
    {
        // var input = ErEngine.Input;
        // double x = 0;
        // double y = 0;
        // if(input.GetKeyDown(SDL3.SDL.Scancode.A)) x-=1;
        // if(input.GetKeyDown(SDL3.SDL.Scancode.D)) x+=1;
        // if(input.GetKeyDown(SDL3.SDL.Scancode.W)) y-=1;
        // if(input.GetKeyDown(SDL3.SDL.Scancode.S)) y+=1;
        // Velocity = new ErVec2(x,y) * Speed;
        // ErEngine.Log("player pos: ", Position);
        base.Update();
        // SwGame.Camera.Position = Position;
        SwGame.SetPlayerPos(Position);
    }

    protected override void DrawImpl(SwEntity nextState)
    {
        // ErEngine.Log(Position);
        // double ft = ErEngine.FrameTime;
        // double ftr = ErEngine.FrameTimeRemaining;
        // double weight = ftr / ft;
        // ErEngine.Log("ft: ", ft, " ftr: ", ftr, " weight: ", weight);
        // Texture.Draw(ErMath.Lerp(Position, nextState.Position, SwGame.FrameProgress));
    }
    // public override SwEntity New()
    // {
    //     return new SwPlayer();
    // }
}