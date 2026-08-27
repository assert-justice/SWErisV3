using Eris;
using Eris.Renderer;
using ErisMath;

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
    private double SpawnDelay = 0;
    private readonly Stack<int> ToRemove = [];
    private double CurrentTime;
    private readonly SwAnimation Animation;
    public ErVec2 Origin;
    public int Amount = 8;
    public double Lifetime = 1;
    public double LifetimeRandomness = 0;
    public double Speed = 300;
    public double Angle = 0;
    public double AngleRandomness = ErMath.PI;
    public double Explosiveness = 0;
    public bool OneShot = false;
    public bool UseLocalCoords = true;
    public bool Emitting = false;
    public bool RandomizeFrames = false;
    public SwParticles2D(SwAnimation animation)
    {
        Animation = animation;
    }
    private void AddParticle()
    {
        // create new particle
        if(UseLocalCoords) Positions.Add(ErVec2.Zero);
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
    public void Update(double dt)
    {
        CurrentTime += dt;
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
        ErVec2 origin = (UseLocalCoords ? Origin : ErVec2.Zero) - Animation.Size * 0.5;
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
}
