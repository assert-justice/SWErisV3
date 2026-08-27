using Eris;
using ErisMath;
using SpoonWitch.ByteStream;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.Component;

public class SwParticleComponent : SwComponent
{
    private int Id;
    private SwAnimation Animation;
    public SwParticles2D? Particles{get; private set;}
    public ErVec2 Offset = ErVec2.Zero;
    public SwParticleComponent(SwEntity parent, string name, SwAnimation animation) : base(parent, name)
    {
        Animation = animation;
    }
    public override void Ready()
    {
        base.Ready();
        Id = SwApp.GetNextId();
        SwParticles2D particles = new(Animation);
        if(!SwGame.ParticleEmitters.TryAdd(Id, particles))
        {
            ErEngine.LogWarning("bad particle id");
            return;
        }
    }
    public override void Update()
    {
        base.Update();
        if(Particles is not null)
        {
            Particles.Origin = Parent.Position + Offset;
            Particles.Update(SwGame.DeltaTime);
        }
    }
    public override void Draw(SwComponent nextState)
    {
        base.Draw(nextState);
        Particles?.Draw(SwGame.FrameDuration * SwGame.FrameWeight);
    }
    public override void Cleanup()
    {
        base.Cleanup();
        SwGame.ParticleEmitters.Remove(Id);
    }
    public override void Write(SwByteStream byteStream)
    {
        base.Write(byteStream);
        byteStream.WriteI32(Id);
        byteStream.WriteVec2(Offset);
        Particles = null;
    }
    public override void Read(SwByteStream byteStream)
    {
        base.Read(byteStream);
        byteStream.TryReadI32(out Id);
        byteStream.TryReadVec2(out Offset);
        if(!SwGame.ParticleEmitters.TryGetValue(Id, out var p)) return;
        Particles = p;
    }
}