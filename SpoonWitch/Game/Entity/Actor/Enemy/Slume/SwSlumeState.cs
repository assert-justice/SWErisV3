using Eris;
using ErisMath;
using SpoonWitch.Game.Entity.Actor.Player;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Slume;

public abstract class SwSlumeState: SwState<SwSlume>
{
    private SwSprite BodySprite = null!;
    private SwAreaComponent Hurtbox = null!;
    private static readonly string[] DirStrings = [
        "move_dr",
        "move_d",
        "move_dl",
        "move_u",
    ];
    public override void Ready()
    {
        base.Ready();
        BodySprite = Entity.GetComponent<SwSpriteComponent>("body")?.Sprite!;
        Hurtbox = Entity.GetComponent<SwAreaComponent>("hurtbox")!;
    }
    private void PlayBodyAnim()
    {
        int facingIdx = ErMath.RoundAngleToInt(Entity.Velocity.GetAngle(), 4);
        BodySprite.Play(DirStrings[facingIdx]);
    }
    // public override void BeginState(string lastState)
    // {
    //     base.BeginState(lastState);
    //     ErEngine.Log(Name);
    // }
    private class Default : SwSlumeState
    {
        public override string Name => "default";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            BodySprite.Play("idle_d");
            Entity.Velocity = ErVec2.Zero;
        }
        public override void Update(double dt)
        {
            base.Update(dt);
        }
    }
    private class Chasing : SwSlumeState
    {
        public override string Name => "chasing";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            if(Entity.TryGetClosestEntity<SwPlayer>(out var entity))
            {
                Entity.TargetEntity = entity;
            }
        }
        public override void Update(double dt)
        {
            base.Update(dt);
            var target = Entity.TargetEntity;
            if(target is null || !Entity.CanSeePoint(target.Position))StateMachine.SetState("seeking");
            else
            {
                Entity.MoveToPoint(target.Position, Entity.BaseSpeed);
                // Note: we are also setting the target position because if we lose sight of the target, the seek state will use it
                Entity.TargetPosition = target.Position;
            }
            PlayBodyAnim();
        }
    }
    private class Fleeing : SwSlumeState
    {
        public override string Name => "fleeing";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            if(Entity.TryGetClosestEntity<SwPlayer>(out var entity))
            {
                Entity.TargetEntity = entity;
            }
        }
        public override void Update(double dt)
        {
            base.Update(dt);
            var target = Entity.TargetEntity;
            if(target is null || !Entity.CanSeePoint(target.Position))StateMachine.SetDefaultState();
            else Entity.MoveToPoint(target.Position, -Entity.BaseSpeed);
            PlayBodyAnim();
        }
    }
    private class Seeking: SwSlumeState
    {
        public override string Name => "seeking";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Entity.TimeoutClock.Start(Entity.SeekGiveUpTime);
        }
        private bool ShouldGiveUp()
        {
            if(!Entity.Velocity.IsNonzero()) return true;
            if(!Entity.TimeoutClock.IsRunning) return true;
            if(Entity.IsPointWithinRadius(Entity.TargetPosition, 16)) return true;
            return false;
        }
        public override void Update(double dt)
        {
            base.Update(dt);
            if(Entity.CanSeeAnyPlayer())StateMachine.SetState("chasing");
            else if(ShouldGiveUp()) StateMachine.SetState("wandering");
            else Entity.MoveToTarget(Entity.BaseSpeed);
            PlayBodyAnim();
        }
    }
    private class Wandering: SwSlumeState
    {
        public override string Name => "wandering";
        private bool TryRandomTarget()
        {
            // Todo: optimize this
            double angle = Random.Shared.NextDouble() * ErMath.TAU;
            var dir = ErVec2.FromAngle(angle) * 128;
            var pos = dir + Entity.Position;
            if(!Entity.CanSeePoint(pos)) return false;
            if(!Entity.Game.Map.InSameRoom(Entity.Position, pos)) return false;
            Entity.TargetPosition = pos;
            Entity.TimeoutClock.Start(1);
            return true;
        }
        private void SetNewWander()
        {
            for (int i = 0; i < 50; i++)
            {
                if(TryRandomTarget()) return;
            }
            ErEngine.LogWarning("slume could not find target pos");
        }
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            SetNewWander();
            Hurtbox.Enabled = true;
        }
        public override void Update(double dt)
        {
            base.Update(dt);
            if(Entity.CanSeeAnyPlayer())StateMachine.SetState("chasing");
            else if(Entity.TimeoutClock.IsRunning)
            {
                Entity.MoveToTarget(Entity.BaseSpeed * Entity.WanderSpeedMul);
            }
            else
            {
                // pick a new random wander point
                SetNewWander();
            }
            PlayBodyAnim();
        }
    }
    private class Dead: SwSlumeState
    {
        public override string Name => "dead";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            BodySprite.Play("death");
            Entity.Velocity = ErVec2.Zero;
            Hurtbox.Enabled = false;
        }
        public override void EndState(string nextState)
        {
            base.EndState(nextState);
            ErEngine.LogWarning("slume attempted to leave death state! ", nextState);
        }
    }
    private class Knockback: SwSlumeState
    {
        public override string Name => "knockback";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            // Note: use the first frame of the death animation
            BodySprite.Play("death");
            BodySprite.Stop();
        }
        public override void Update(double dt)
        {
            base.Update(dt);
            double speed = Entity.Velocity.GetLength();
            if(speed > ErMath.EPSILON) Entity.Velocity = Entity.Velocity.Normalized() * speed * 0.95;
            if(Entity.IsKnockback) return;
            if(!Entity.IsAlive) StateMachine.SetState("dead");
            else if(Entity.IsPassive) StateMachine.SetDefaultState();
            else if(Entity.Health < Entity.MaxHealth * Entity.FleeThreshold) StateMachine.SetState("fleeing");
            else StateMachine.SetDefaultState();
        }
    }
    public static SwStateMachine<SwSlume> GetStateMachine(SwSlume parent, string name)
    {
        return new(parent, name, [
            new Default(),
            new Fleeing(),
            new Chasing(),
            new Seeking(),
            new Wandering(),
            new Knockback(),
            new Dead(),
        ]);
    }
}
