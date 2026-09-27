using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Utils;

namespace SpoonWitch.Rendering;

public class SwParticles2D
{
    private struct ParticleData
    {
        public ErVec2 Velocity;
        public double Expires;
    }
    private readonly List<ErVec2> Positions = [];
    private readonly List<ParticleData> DataEntries = [];
    private readonly List<SwAnimationState> AnimStates = [];
    private readonly Queue<double> SpawnQueue = [];
    private readonly Stack<int> ToRemove = [];
    private readonly SwAnimation Animation;
    public int LiveParticles => Positions.Count;
    private double SpawnDelay = 0;
    private double CurrentTime;
    public ErVec2 Origin;
    public ErVec2 Offset;
    public int Amount = 8;
    public double Lifetime = 1;
    public double LifetimeRandomness = 0;
    public double Speed = 300;
    public double Angle = 0;
    public double AngleRandomness = ErMath.PI;
    public double Explosiveness = 0;
    public bool OneShot = false;
    public bool UseLocalCoordinates = true;
    public bool Emitting = false;
    public bool RandomizeFrames = false;
    public SwParticles2D(SwAnimation animation)
    {
        Animation = animation;
    }
    public SwParticles2D(ErTexture texture)
    {
        var frames = SwFrame.GetAllFrames(new(texture), texture.Size);
        Animation = new SwAnimation("default", [..frames], texture.Size, new SwAnimationState());
    }
    private void AddParticle()
    {
        // create new particle
        if(UseLocalCoordinates) Positions.Add(ErVec2.Zero);
        else Positions.Add(Origin);
        // calc expiration
        double lifetimeMul = Random.Shared.NextDouble() * 2 - 1;
        double lifetime = Lifetime + Lifetime * lifetimeMul * LifetimeRandomness;
        double expires = CurrentTime + lifetime;
        // calc random velocity
        double angle = Angle + (Random.Shared.NextDouble() * 2 -1) * AngleRandomness;
        ErVec2 vel = ErVec2.FromAngle(angle) * Speed;
        DataEntries.Add(new(){Velocity = vel, Expires = expires});
        SwAnimationState state = Animation.DefaultState;
        SwAnimationState.Set(ref state, isPlaying: true);
        if(RandomizeFrames) SwAnimationState.Set(ref state, frameIdx: ErMath.FloorToInt(Random.Shared.NextDouble() * Animation.NumFrames));
        AnimStates.Add(state);
    }
    private void QueueParticles(int quantity)
    {
        double delay = Lifetime / Amount * (1-Explosiveness);
        for (int idx = 0; idx < quantity; idx++)
        {
            SpawnQueue.Enqueue(delay);
        }
    }
    private void UpdateNoAdvance(double dt)
    {
        for (int idx = 0; idx < DataEntries.Count; idx++)
        {
            if(DataEntries[idx].Expires > CurrentTime)
            {
                // update particle position
                Positions[idx] += DataEntries[idx].Velocity * dt;
            }
            else
            {
                // queue particle for removal
                ToRemove.Push(idx);
            }
        }
    }
    private void UpdateAdvance(double dt)
    {
        SwAnimationState state = default;
        for (int idx = 0; idx < DataEntries.Count; idx++)
        {
            if(DataEntries[idx].Expires > CurrentTime)
            {
                // update particle position
                Positions[idx] += DataEntries[idx].Velocity * dt;
                state = AnimStates[idx];
                SwAnimationState.Advance(ref state, dt, Animation.NumFrames);
                AnimStates[idx] = state;
            }
            else
            {
                // queue particle for removal
                ToRemove.Push(idx);
            }
        }
    }
    public void Update(double dt)
    {
        CurrentTime += dt;
        if(RandomizeFrames) UpdateNoAdvance(dt);
        else UpdateAdvance(dt);
        while(ToRemove.TryPop(out int idx))
        {
            if(idx < Positions.Count - 1)
            {
                Positions[idx] = Positions[^1];
                DataEntries[idx] = DataEntries[^1];
                AnimStates[idx] = AnimStates[^1];
            }
            Positions.RemoveAt(Positions.Count - 1);
            DataEntries.RemoveAt(DataEntries.Count - 1);
            AnimStates.RemoveAt(AnimStates.Count - 1);
        }
        if(!Emitting) return;
        // determine if we need to emit more particles
        int particlesNeeded = Amount - Positions.Count - SpawnQueue.Count;
        QueueParticles(particlesNeeded);
        // add new particles
        if(SpawnDelay <= 0 && SpawnQueue.Count == 0) return;
        SpawnDelay -= dt;
        while(SpawnDelay <= 0 && SpawnQueue.TryDequeue(out double delay))
        {
            AddParticle();
            SpawnDelay += delay;
        }
        if(Emitting && OneShot && SpawnDelay <= 0 && SpawnQueue.Count == 0) Emitting = false;
    }
    public void Draw(double dt)
    {
        ErVec2 origin = (UseLocalCoordinates ? Origin : ErVec2.Zero) - Animation.Size * 0.5 + Offset;
        SwAnimationState state = default;
        for (int idx = 0; idx < Positions.Count; idx++)
        {
            var pos = Positions[idx] + DataEntries[idx].Velocity * dt + origin;
            state = AnimStates[idx]; 
            SwAnimationState.Advance(ref state, dt, Animation.NumFrames);
            if(!Animation.TryGetFrame(out var frame, state.FrameIdx)) continue;
            frame.Draw(pos);
        }
    }
    public static bool TryFromData(out SwParticles2D particles, PriNode data)
    {
        particles = default!;
        if(data.TryGet("ase_data", out PriDict dict))
        {
            if(!dict.TryGet("name", out string name)) return ErEngine.LogWarning("no name found for particle ase animation");
            if(!SwAseImporter.TryFromPriData(out var aseImporter, dict.Get("filepath"))) return false;
            if(!aseImporter.TryGetAnimation(out var animation, name)) return ErEngine.LogWarning("invalid name for particle ase animation");
            particles = new(animation);
        }
        else if(data.TryGet("filepath_texture", out string filepath))
        {
            if(!ErTexture.TryFromPath(filepath, out var texture)) return false;
            particles = new(texture);
        }
        if(data.TryGet("spawn_delay", out double d)) particles.SpawnDelay = d;
        if(SwPrion.TryGetVec2(out var v, data.Get("origin"))) particles.Origin = v;
        if(SwPrion.TryGetVec2(out v, data.Get("offset"))) particles.Offset = v;
        if(data.TryGet("amount", out int i)) particles.Amount = i;
        if(data.TryGet("lifetime", out d)) particles.Lifetime = d;
        if(data.TryGet("lifetime_randomness", out d)) particles.LifetimeRandomness = d;
        if(data.TryGet("speed", out d)) particles.Speed = d;
        if(data.TryGet("angle", out d)) particles.Angle = d;
        if(data.TryGet("angle_randomness", out d)) particles.AngleRandomness = d;
        if(data.TryGet("explosiveness", out d)) particles.Explosiveness = d;
        if(data.TryGet("one_shot", out bool b)) particles.OneShot = b;
        if(data.TryGet("use_local_coordinates", out b)) particles.UseLocalCoordinates = b;
        if(data.TryGet("emitting", out b)) particles.Emitting = b;
        if(data.TryGet("randomize_frames", out b)) particles.RandomizeFrames = b;
        return particles is not null;
    }
}
