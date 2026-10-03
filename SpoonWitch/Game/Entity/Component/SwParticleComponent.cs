using Eris;
using ErisMath;
using SpoonWitch.ByteStream;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.Component;

public class SwParticleComponent : SwComponent
{
    public SwParticleComponent(SwEntity parent, string name, SwParticles2D particles) : base(parent, name)
    {
        Particles = particles;
    }

    public SwParticles2D Particles{get; private set;} = null!;
    public override void Update(double dt)
    {
        base.Update(dt);
        Particles.Origin = Parent.Position;
        Particles.Update(dt);
    }
    public override void Draw()
    {
        base.Draw();
        Particles.Draw(0);
    }
}