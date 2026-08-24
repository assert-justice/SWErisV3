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
    private readonly Queue<double> SpawnQueue = [];
    private double SpawnDelay = -1;
    private readonly Stack<int> ToRemove = [];
    private double CurrentTime;
    public readonly ErTexture Texture;
    public ErVec2 Origin;
    public int Amount = 8;
    public double Lifetime = 1;
    public double LifetimeRandomness;
    public double Speed = 300;
    public double Explosiveness = 0;
    public bool Emitting = false;
    public SwParticles2D(ErTexture texture)
    {
        Texture = texture;
    }
    private void AddParticle()
    {
        // create new particle
        Positions.Add(ErVec2.Zero);
        // calc expiration
        double lifetimeMul = Random.Shared.NextDouble() * 2 - 1;// * LifetimeRandomness * 0.5;
        double lifetime = Lifetime + Lifetime * lifetimeMul * LifetimeRandomness;
        double expires = CurrentTime + lifetime;
        // calc random velocity
        double angle = Random.Shared.NextDouble() * ErMath.TAU;
        ErVec2 vel = ErVec2.FromAngle(angle) * Speed;
        DataEntries.Add(new(){Velocity = vel, Expires = expires});
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
        while(ToRemove.TryPop(out int idx))
        {
            if(idx < Positions.Count - 1)
            {
                Positions[idx] = Positions[^1];
                DataEntries[idx] = DataEntries[^1];
            }
            Positions.RemoveAt(Positions.Count - 1);
            DataEntries.RemoveAt(DataEntries.Count - 1);
        }
        if(!Emitting) return;
        // determine if we need to emit more particles
        int particlesNeeded = Amount - Positions.Count;
        if(SpawnQueue.Count == 0 && particlesNeeded > 0)
        {
            QueueParticles(particlesNeeded);
            
        }
        // add new particles
        if(SpawnDelay <= 0 && SpawnQueue.Count == 0) return;
        while (true)
        {
            if (SpawnDelay <= 0)
            {
                if (!SpawnQueue.TryDequeue(out double delay)) break;
                SpawnDelay = delay;
            }
            SpawnDelay -= dt;
            if (SpawnDelay > 0)
            {
                break;
            }
            AddParticle();
        }
    }
    public void Draw(double dt)
    {
        for (int idx = 0; idx < Positions.Count; idx++)
        {
            var pos = Positions[idx] + DataEntries[idx].Velocity * dt + Origin;
            Texture.DrawQuick(pos);
        }
    }
}
