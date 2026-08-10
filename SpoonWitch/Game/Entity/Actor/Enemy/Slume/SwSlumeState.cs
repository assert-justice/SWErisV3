using Eris;
using Eris.Utils;
using ErisMath;
using SpoonWitch.ByteStream;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Slume;

public abstract class SwSlumeState: SwEntState<SwSlume>
{
    private SwSprite BodySprite = null!;
    private static readonly string[] DirStrings = [
        "move_dr",
        "move_d",
        "move_dl",
        "move_u",
    ];
    public override void Init(SwStateMachine stateMachine)
    {
        base.Init(stateMachine);
        BodySprite = Entity.GetComponent<SwSpriteComponent>("body")?.Sprite!;
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
    // public override void Update()
    // {
    //     base.Update();
    //     if(Slume.IsKnockback) ErEngine.Log(Name, " ", Slume.Velocity);
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
        public override void Update()
        {
            base.Update();
        }
    }
    private class Chasing : SwSlumeState
    {
        public override string Name => "chasing";
        public override void Update()
        {
            base.Update();
            Entity.TargetPosition = SwGame.PlayerPos;
            if(!Entity.CanSeePlayer())StateMachine.SetState("seeking");
            Entity.MoveToTarget(Entity.BaseSpeed);
            Entity.DoDamage();
            PlayBodyAnim();
        }
    }
    private class Seeking: SwSlumeState
    {
        public override string Name => "seeking";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Entity.TimeoutClock = 1;
        }
        public override void Update()
        {
            base.Update();
            if(Entity.CanSeePlayer())StateMachine.SetState("chasing");
            else if(Entity.TimeoutClock > 0) Entity.TimeoutClock -= SwGame.DeltaTime;
            else StateMachine.SetState("wandering");
            Entity.MoveToTarget(Entity.BaseSpeed);
            Entity.DoDamage();
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
            if(!SwGame.GetMap().TryGetRoom(pos, out var targetRoom)) return false;
            if(!SwGame.GetMap().TryGetRoom(Entity.Position, out var room)) return false;
            if(targetRoom.Id != room.Id) return false;
            Entity.TargetPosition = pos;
            Entity.TimeoutClock = 1;
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
        }
        public override void Update()
        {
            base.Update();
            if(Entity.CanSeePlayer())StateMachine.SetState("chasing");
            else if(Entity.TimeoutClock > 0)
            {
                Entity.TimeoutClock -= SwGame.DeltaTime;
                Entity.MoveToTarget(Entity.BaseSpeed * Entity.WanderSpeedMul);
            }
            else
            {
                // pick a new random wander point
                SetNewWander();
            }
            Entity.DoDamage();
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
            Entity.TimeoutClock = 1;
            Entity.Velocity = ErVec2.Zero;
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
        public override void Update()
        {
            base.Update();
            double speed = Entity.Velocity.GetLength();
            if(speed > ErMath.EPSILON) Entity.Velocity = Entity.Velocity.Normalized() * speed * 0.95;
            if(Entity.IsKnockback) return;
            if(Entity.IsAlive) StateMachine.SetState(Entity.IsPassive ? "default" : "wandering");
            else StateMachine.SetState("dead");
        }
    }
    public static SwStateMachine GetStateMachine(SwSlume parent, string name)
    {
        return new(parent, name, [
            new Default(),
            new Chasing(),
            new Seeking(),
            new Wandering(),
            new Knockback(),
            new Dead(),
        ]);
    }
}