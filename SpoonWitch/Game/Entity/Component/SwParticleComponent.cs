using Eris;
using ErisMath;
using SpoonWitch.ByteStream;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.Component;

public class SwParticleComponent : SwComponent
{
    private int Id;
    private SwAnimation Animation;
    public SwParticles2D Particles{get; private set;} = null!;
    public ErVec2 Offset = ErVec2.Zero;
    public SwParticleComponent(SwEntity parent, string name, SwAnimation animation) : base(parent, name)
    {
        Animation = animation;
    }
    public override void Ready()
    {
        base.Ready();
        Id = SwApp.GetNextId();
        Particles = new(Animation);
    }
    public override void Update()
    {
        base.Update();
        Particles.Origin = Parent.Position + Offset;
        Particles.Update(SwGame.DeltaTime);
    }
    public override void Draw(SwComponent nextState)
    {
        base.Draw(nextState);
        Particles.Draw(SwGame.FrameDuration * SwGame.FrameWeight);
    }
}