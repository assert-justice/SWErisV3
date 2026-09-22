using Eris;
using ErisMath;
using SpoonWitch.ByteStream;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.Component;

public class SwParticleComponent : SwComponent
{
    public SwParticles2D Particles{get; private set;} = null!;
    public SwParticleComponent(SwEntity parent, string name, SwParticles2D particles) : base(parent, name)
    {
        Particles = particles;
    }
    public override void Update()
    {
        base.Update();
        Particles.Origin = Parent.Position;
        Particles.Update(SwGame.DeltaTime);
    }
    public override void Draw(SwComponent nextState)
    {
        base.Draw(nextState);
        Particles.Draw(SwGame.FrameDuration * SwGame.FrameWeight);
    }
}