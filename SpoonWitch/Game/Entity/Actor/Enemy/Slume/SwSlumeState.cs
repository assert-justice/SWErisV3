using Eris;
using Eris.Utils;
using ErisMath;
using SpoonWitch.ByteStream;
using SpoonWitch.Game.Entity.Component.Sprite;
using SpoonWitch.Game.Entity.Component.State;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Slume;

public abstract class SwSlumeState(SwSlume parent) : SwState(parent)
{
    protected readonly SwSlume Slume = parent;
    protected ErWrapper<SwSprite> Sprite = new(() => parent.GetComponent<SwSprite>("body")!);
    protected ErWrapper<SwStateMachine> StateMachine = new(() => parent.GetComponent<SwStateMachine>("state_machine")!);
    private static readonly string[] DirStrings = [
        "move_dr",
        "move_d",
        "move_dl",
        "move_u",
    ];
    private void PlayBodyAnim()
    {
        int facingIdx = ErMath.RoundAngleToInt(Slume.Velocity.GetAngle(), 4);
        Sprite.Value.Play(DirStrings[facingIdx]);
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
    private class Default(SwSlume parent) : SwSlumeState(parent)
    {
        public override string Name => "default";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Sprite.Value.Play("idle_d");
            Slume.Velocity = ErVec2.Zero;
        }
        public override void Update()
        {
            base.Update();
            // if(Slume.CanSeePlayer())StateMachine.Value.SetState("chasing");
        }
    }
    private class Chasing(SwSlume parent) : SwSlumeState(parent)
    {
        public override string Name => "chasing";
        public override void Update()
        {
            base.Update();
            Slume.TargetPosition = SwGame.PlayerPos;
            if(!Slume.CanSeePlayer())StateMachine.Value.SetState("seeking");
            Slume.MoveToTarget(Slume.BaseSpeed);
            Slume.DoDamage();
            PlayBodyAnim();
        }
    }
    private class Seeking(SwSlume parent) : SwSlumeState(parent)
    {
        public override string Name => "seeking";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Slume.TimeoutClock = 1;
        }
        public override void Update()
        {
            base.Update();
            if(Slume.CanSeePlayer())StateMachine.Value.SetState("chasing");
            else if(Slume.TimeoutClock > 0) Slume.TimeoutClock -= SwGame.DeltaTime;
            else StateMachine.Value.SetState("wandering");
            Slume.MoveToTarget(Slume.BaseSpeed);
            Slume.DoDamage();
            PlayBodyAnim();
        }
    }
    private class Wandering(SwSlume parent) : SwSlumeState(parent)
    {
        public override string Name => "wandering";
        private bool TryRandomTarget()
        {
            // Todo: optimize this
            double angle = Random.Shared.NextDouble() * ErMath.TAU;
            var dir = ErVec2.FromAngle(angle) * 128;
            var pos = dir + Slume.Position;
            if(!Slume.CanSeePoint(pos)) return false;
            if(!SwGame.GetMap().TryGetRoom(pos, out var targetRoom)) return false;
            if(!SwGame.GetMap().TryGetRoom(Slume.Position, out var room)) return false;
            if(targetRoom.Id != room.Id) return false;
            Slume.TargetPosition = pos;
            Slume.TimeoutClock = 1;
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
            if(Slume.CanSeePlayer())StateMachine.Value.SetState("chasing");
            else if(Slume.TimeoutClock > 0)
            {
                Slume.TimeoutClock -= SwGame.DeltaTime;
                Slume.MoveToTarget(Slume.BaseSpeed * Slume.WanderSpeedMul);
            }
            else
            {
                // pick a new random wander point
                SetNewWander();
            }
            Slume.DoDamage();
            PlayBodyAnim();
        }
    }
    private class Dead(SwSlume parent) : SwSlumeState(parent)
    {
        public override string Name => "dead";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Sprite.Value.Play("death");
            Slume.TimeoutClock = 1;
            Slume.Velocity = ErVec2.Zero;
        }
        public override void EndState(string nextState)
        {
            base.EndState(nextState);
            ErEngine.LogWarning("slume attempted to leave death state! ", nextState);
        }
    }
    private class Knockback(SwSlume parent) : SwSlumeState(parent)
    {
        public override string Name => "knockback";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            // Note: use the first frame of the death animation
            Sprite.Value.Play("death");
            Sprite.Value.Stop();
        }
        public override void Update()
        {
            base.Update();
            double speed = Slume.Velocity.GetLength();
            if(speed > ErMath.EPSILON) Slume.Velocity = Slume.Velocity.Normalized() * speed * 0.95;
            if(Slume.IsKnockback) return;
            if(Slume.IsAlive) StateMachine.Value.SetState(Slume.IsPassive ? "default" : "wandering");
            else StateMachine.Value.SetState("dead");
        }
    }
    public static SwStateMachine GetStateMachine(SwSlume parent, string name)
    {
        return new(parent, name, [
            new Default(parent),
            new Chasing(parent),
            new Seeking(parent),
            new Wandering(parent),
            new Knockback(parent),
            new Dead(parent),
        ]);
    }
}