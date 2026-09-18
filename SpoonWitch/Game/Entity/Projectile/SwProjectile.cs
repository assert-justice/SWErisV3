using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;

namespace SpoonWitch.Game.Entity.Projectile;

public class SwProjectile : SwEntity
{
    private readonly ErTexture Texture;
    private uint CollisionMask = 0;
    public ErVec2 Velocity;
    public SwProjectile()
    {
        SwAreaComponent hurtbox = new(this, "hurtbox", 4, new(14,14), enabled:true, onBodyEnter: OnEnterHurtbox);
        RegisterComponent(hurtbox);
        if(!ErTexture.TryFromPath("game_data/entities/actors/player/images/bella_sling_ammo_shot.png", out Texture)) ErEngine.LogError("bad projectile texture path");
    }
    public override void Ready()
    {
        base.Ready();
        Props.TryGet("x_velocity", out double xVel);
        Props.TryGet("y_velocity", out double yVel);
        Velocity = new(xVel, yVel);
        if(Props.TryGet("collision_mask", out uint i)) CollisionMask = i;
    }
    public override void Update()
    {
        base.Update();
        Position += Velocity * SwGame.DeltaTime;
        var tileCoord = SwGame.Map.PhysicsWorld.PointToTileCoord(Position);
        int tileId = SwGame.Map.GetTopTile(tileCoord);
        if(tileId < 0) return;
        var tileData = SwGame.TileData[tileId];
        if((tileData.CollisionMask & CollisionMask) != 0) QueueFree();
    }
    protected override void DrawImpl(SwEntity nextState)
    {
        base.DrawImpl(nextState);
        var pos = ErMath.Lerp(Position, nextState.Position, SwGame.FrameWeight) - Texture.Size * 0.5;
        Texture.Draw(pos);
    }
    private void OnEnterHurtbox(SwEntity entity)
    {
        if(!Props.TryGet("damage", out PriNode damage)) return;
        entity.AddCommand(damage);
    }
}