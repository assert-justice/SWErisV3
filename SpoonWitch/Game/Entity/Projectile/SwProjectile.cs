using Eris;
using Eris.Renderer;
using ErisMath;
using SpoonWitch.ByteStream;
using SpoonWitch.Game.Entity.Component;

namespace SpoonWitch.Game.Entity.Projectile;

public class SwProjectile : SwEntity, ISwEntity<SwProjectile>
{
    public static byte TypeId => 3;
    private static SwProjectile? _Primary;
    private static SwProjectile? _Secondary;
    public static SwProjectile Primary => _Primary ??= new();
    public static SwProjectile Secondary => _Secondary ??= new();
    protected override byte GetTypeId => TypeId;
    private readonly ErTexture Texture;
    // public override uint Mask => 0;
    public SwProjectile()
    {
        SwAreaComponent hurtbox = new(this, "hurtbox", 4, new(14,14), enabled:true);
        // hurtbox.Area.OnBodyEnterFn = OnEnterSpoonHurtbox;
        RegisterComponent(hurtbox);
        if(!ErTexture.TryFromPath("game_data/entities/actors/player/images/bella_sling_ammo_shot.png", out Texture)) ErEngine.LogError("bad projectile texture path");
    }
    public override void Ready()
    {
        base.Ready();
        // ErEngine.Log(EntProps.Props);
        EntProps.Props.TryGet("x_velocity", out double xVel);
        EntProps.Props.TryGet("y_velocity", out double yVel);
        Velocity = new(xVel, yVel);
        // if(Position.IsNonzero() || Velocity.IsNonzero())ErEngine.Log("pos: ", Position, " vel: ", Velocity);
    }
    public override void Update()
    {
        base.Update();
        var tileCoord = SwGame.Map.PhysicsWorld.PointToTileCoord(Position);
        var tileId = SwGame.Map.PhysicsWorld.GetTile(tileCoord);
        var tileData = SwGame.Map.GetTileData(tileId);
        if(tileData.IsOpaque) QueueFree();
        // EntProps.Props.TryGet("x_velocity", out double xVel);
        // EntProps.Props.TryGet("y_velocity", out double yVel);
        // Velocity = new(xVel, yVel);
        // if(Position.IsNonzero() || Velocity.IsNonzero())ErEngine.Log("pos: ", Position, " vel: ", Velocity);
        // ErEngine.Log(EntProps.Props);
    }
    protected override void DrawImpl(SwEntity nextState)
    {
        base.DrawImpl(nextState);
        var pos = ErMath.Lerp(Position, nextState.Position, SwGame.FrameWeight) - Texture.Size * 0.5;
        Texture.Draw(pos);
    }
    public override void GameCleanup()
    {
        base.GameCleanup();
        
    }
}