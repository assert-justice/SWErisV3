using Eris;
using Eris.Renderer;
using ErisMath;
using ErisPhysics2D.Collider;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Map.Collision;

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
    public SwProjectile()
    {
        SwAreaComponent hurtbox = new(this, "hurtbox", 4, new(14,14), enabled:true);
        hurtbox.Area.OnBodyEnterFn = OnEnterHurtbox;
        RegisterComponent(hurtbox);
        if(!ErTexture.TryFromPath("game_data/entities/actors/player/images/bella_sling_ammo_shot.png", out Texture)) ErEngine.LogError("bad projectile texture path");
    }
    public override void Ready()
    {
        base.Ready();
        EntProps.Props.TryGet("x_velocity", out double xVel);
        EntProps.Props.TryGet("y_velocity", out double yVel);
        Velocity = new(xVel, yVel);
    }
    public override void Update()
    {
        base.Update();
        var tileCoord = SwGame.Map.PhysicsWorld.PointToTileCoord(Position);
        var tileId = SwGame.Map.PhysicsWorld.GetTile(tileCoord);
        var tileData = SwGame.Map.GetTileData(tileId);
        if(tileData.IsOpaque) QueueFree();
    }
    protected override void DrawImpl(SwEntity nextState)
    {
        base.DrawImpl(nextState);
        var pos = ErMath.Lerp(Position, nextState.Position, SwGame.FrameWeight) - Texture.Size * 0.5;
        Texture.Draw(pos);
    }
    private static void OnEnterHurtbox(SwColliderArea area, int bodyId, ErColliderBody body)
    {
        if(!SwGame.TryGetEntProps(area.ParentId, out var myProps)) return;
        if(!SwGame.TryGetEntProps(body.ParentId, out var targetProps)) return;
        if(!myProps.Props.TryGet("damage", out PriNode damage)) return;
        targetProps.AddCommand(damage);
    }
}