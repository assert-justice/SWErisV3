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
    private uint HurtboxMask = 0;
    private ErVec2 Size;
    public ErVec2 Velocity;
    public bool Piercing = false;
    private bool IsAlive = true;
    public override int RenderLayer => 3;
    private SwAreaComponent Hurtbox = null!;
    public override void SetProps(PriNode props)
    {
        base.SetProps(props);
        if(Props.TryGet("hurtbox_mask", out uint u)) HurtboxMask = u;
        Size = SwPrion.GetVec2(Props.Get("size"));
        if(Hurtbox is not null)
        {
            Hurtbox.Mask = HurtboxMask;
            Hurtbox.Size = Size;
        }
        if(Props.TryGet("piercing", out bool b)) Piercing = b;
        Velocity = SwPrion.GetVec2(Props.Get("velocity"));
    }
    public override void Init()
    {
        base.Init();
        Hurtbox = new(this, "hurtbox", HurtboxMask, Size, enabled:true, onBodyEnter: OnEnterHurtbox);
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
    protected override void Update(double dt)
    {
        base.Update(dt);
        if (!IsAlive)
        {
            if(ImpactParticles is null || ImpactParticles.Particles.LiveParticles == 0) QueueFree();
            return;
        }
        Position += Velocity * dt;
        uint tileMask = Game.PhysicsWorld.GetTileMaskAtPoint(Position);
        uint overlap = (uint)SwCollisionMask.BlocksNav & tileMask;
        if(overlap != 0)
        {
            Impact();
        }
    }
    protected override void Draw()
    {
        base.Draw();
        if(IsAlive && Texture is not null)
        {
            Texture.Draw(Position);
        }
    }
    private void OnEnterHurtbox(SwEntity entity)
    {
        if(!Props.TryGet("damage", out PriNode damage)) return;
        entity.AddCommand(damage);
        if(!Piercing) Impact();
    }
}