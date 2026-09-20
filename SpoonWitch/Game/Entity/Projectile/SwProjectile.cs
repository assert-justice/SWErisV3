using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.Projectile;

public class SwProjectile : SwEntity
{
    private ErTexture? Texture;
    private SwParticles2D? ImpactParticles;
    private SwParticles2D? FlyingParticles;
    private uint CollisionMask = 0;
    public ErVec2 Velocity;
    public bool Piercing = false;
    private bool IsAlive = true;
    public override int RenderLayer => 3;
    private readonly SwAreaComponent Hurtbox;
    public SwProjectile()
    {
        Hurtbox = new(this, "hurtbox", 4, new(14,14), enabled:true, onBodyEnter: OnEnterHurtbox);
        RegisterComponent(Hurtbox);
            // if(!ErTexture.TryFromPath("game_data/entities/actors/player/images/bella_sling_ammo_shot.png", out Texture)) ErEngine.LogError("bad projectile texture path");
    }
    public override void Ready()
    {
        base.Ready();
        Props.TryGet("x_velocity", out double xVel);
        Props.TryGet("y_velocity", out double yVel);
        Velocity = new(xVel, yVel);
        if(Props.TryGet("collision_mask", out uint i)) CollisionMask = i;
        if(Props.TryGet("texture_filepath", out string texture_filepath))
        {
            if(!ErTexture.TryFromPath(texture_filepath, out Texture)) ErEngine.LogWarning("bad projectile texture path");
        }
        if(Props.TryGet("impact_particles", out PriDict pData))
        {
            if(!SwParticles2D.TryFromData(out ImpactParticles, pData)) ErEngine.LogWarning("bad projectile impact particles at path");
        }
        if(Props.TryGet("flying_particles", out pData))
        {
            if(!SwParticles2D.TryFromData(out FlyingParticles, pData)) ErEngine.LogWarning("bad projectile flying particles at path");
        }
    }
    private void Impact()
    {
        IsAlive = false;
        Hurtbox.Enabled = false;
        ImpactParticles?.Emitting = true;
    }
    public override void Update()
    {
        base.Update();
        if(ImpactParticles is not null)
        {
            ImpactParticles.Origin = Position;
            ImpactParticles.Update(SwGame.DeltaTime);
        }
        if(FlyingParticles is not null)
        {
            FlyingParticles.Origin = Position;
            FlyingParticles.Update(SwGame.DeltaTime);
        }
        if (!IsAlive)
        {
            if(ImpactParticles is null || ImpactParticles.LiveParticles == 0) QueueFree();
            return;
        }
        Position += Velocity * SwGame.DeltaTime;
        var tileCoord = SwGame.Map.PhysicsWorld.PointToTileCoord(Position);
        int tileId = SwGame.Map.GetTopTile(tileCoord);
        if(tileId < 0) return;
        var tileData = SwGame.TileData[tileId];
        if((tileData.CollisionMask & CollisionMask) != 0) Impact();
    }
    protected override void DrawImpl(SwEntity nextState)
    {
        base.DrawImpl(nextState);
        if(IsAlive && Texture is not null)
        {
            var pos = ErMath.Lerp(Position, nextState.Position, SwGame.FrameWeight) - Texture.Size * 0.5;
            Texture.Draw(pos);
        }
        ImpactParticles?.Draw(SwGame.FrameDuration);
        FlyingParticles?.Draw(SwGame.FrameDuration);
    }
    private void OnEnterHurtbox(SwEntity entity)
    {
        if(!Props.TryGet("damage", out PriNode damage)) return;
        entity.AddCommand(damage);
        if(!Piercing) Impact();
    }
}