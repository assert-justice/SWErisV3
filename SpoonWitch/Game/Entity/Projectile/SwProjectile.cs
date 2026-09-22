using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Rendering;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Projectile;

public class SwProjectile : SwEntity
{
    private ErTexture? Texture;
    // private SwAnimation? Animation;
    private SwParticleComponent? ImpactParticles;
    // private SwParticles2D? FlyingParticles;
    private uint CollisionMask = 0;
    public ErVec2 Velocity;
    public bool Piercing = false;
    private bool IsAlive = true;
    public override int RenderLayer => 3;
    private SwAreaComponent Hurtbox = null!;
    protected override void SetProps(PriNode props)
    {
        base.SetProps(props);
        if(Props.TryGet("mask", out uint u)) CollisionMask = u;
        if(Props.TryGet("piercing", out bool b)) Piercing = b;
        Velocity = SwPrion.GetVec2(Props.Get("velocity"));
    }
    public override void Init()
    {
        base.Init();
        Hurtbox = new(this, "hurtbox", 4, new(14,14), enabled:true, onBodyEnter: OnEnterHurtbox);
        RegisterComponent(Hurtbox);
        TryGetBody();
        if(Props.TryGet("impact_particles", out PriDict pData))
        {
            if(!SwParticles2D.TryFromData(out var particles, pData)) ErEngine.LogWarning("bad projectile impact particles at path");
            else
            {
                ImpactParticles = new(this, "impact_particles", particles);
                RegisterComponent(ImpactParticles);
            }
        }
        if(Props.TryGet("flying_particles", out pData))
        {
            if(!SwParticles2D.TryFromData(out var particles, pData)) ErEngine.LogWarning("bad projectile flying particles at path");
            else RegisterComponent(new SwParticleComponent(this, "flying_particles", particles));
        }
    }
    private bool TryGetBody()
    {
        if(!Props.TryGet("body", out PriDict body)) return false;
        if(body.TryGet("texture_filepath", out string texture_filepath)) ErTexture.TryFromPath(texture_filepath, out Texture);
        else if(body.TryGet("ase_data", out PriDict dict))
        {
            if(!dict.TryGet("name", out string name)) return ErEngine.LogWarning("no name found for projectile ase animation");
            if(!SwAseImporter.TryFromPriData(out var aseImporter, dict.Get("filepath"))) return false;
            if(!aseImporter.TryGetAnimation(out var animation, name)) return ErEngine.LogWarning("invalid name for projectile ase animation");
            // Animation = animation;
            SwSprite sprite = new("body");
            sprite.AddAnimation(animation);
            RegisterComponent(new SwSpriteComponent(this, sprite));
        }
        return true;
    }
    private void Impact()
    {
        IsAlive = false;
        Hurtbox.Enabled = false;
        ImpactParticles?.Particles.Emitting = true;
    }
    public override void Update()
    {
        base.Update();
        if (!IsAlive)
        {
            if(ImpactParticles is null || ImpactParticles.Particles.LiveParticles == 0) QueueFree();
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
    }
    private void OnEnterHurtbox(SwEntity entity)
    {
        if(!Props.TryGet("damage", out PriNode damage)) return;
        entity.AddCommand(damage);
        if(!Piercing) Impact();
    }
}