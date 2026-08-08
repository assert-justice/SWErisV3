using Eris;
using Eris.Utils;
using ErisMath;
using SpoonWitch.Game.Entity.Component.Sprite;
using SpoonWitch.Game.Entity.Component.State;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Knight;

public abstract class SwKnightState(SwKnight parent) : SwState(parent)
{
    protected readonly SwKnight Knight = parent;
    protected ErWrapper<SwSprite> BodySprite = new(() => parent.GetComponent<SwSprite>("body")!);
    protected ErWrapper<SwSprite> SwordSprite = new(() => parent.GetComponent<SwSprite>("sword")!);
    protected ErWrapper<SwStateMachine> StateMachine = new(() => parent.GetComponent<SwStateMachine>("state_machine")!);
    private static readonly string[][] BodyAnims = [
        [
            "move_0h_dr",
            "move_0h_d",
            "move_0h_dl",
            "move_0h_u",
        ],
        [
            "move_1h_dr",
            "move_1h_d",
            "move_1h_dl",
            "move_1h_u",
        ],
        [
            "move_2h_dr",
            "move_2h_d",
            "move_2h_dl",
            "move_2h_u",
        ],
    ];
    const double CLOSE_ENOUGH = 5;
    private void PlayBodyAnim(int hands, byte facing)
    {
        BodySprite.Value.Play(BodyAnims[hands][facing]);
    }
    private void PlayBodyAnim(int hands = 2)
    {
        PlayBodyAnim(hands, Knight.FacingIdx);
    }
    private bool NeedsNewTarget()
    {
        if(Knight.TimeoutClock <= 0) return true;
        if(Knight.Velocity.GetLengthSquared() < CLOSE_ENOUGH) return true;
        if(Knight.DistanceToTarget() < CLOSE_ENOUGH) return true;
        return false;
    }
    // public override void BeginState(string lastState)
    // {
    //     base.BeginState(lastState);
    //     ErEngine.Log(Name);
    // }
    private class Default(SwKnight parent) : SwKnightState(parent)
    {
        public override string Name => "default";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Knight.Velocity = ErVec2.Zero;
        }
        public override void Update()
        {
            base.Update();
            BodySprite.Value.Play("move_2h_d");
        }
    }
    private class Wandering(SwKnight parent) : SwKnightState(parent)
    {
        public override string Name => "wandering";
        private bool TryRandomTarget()
        {
            // Todo: optimize this
            double angle = Random.Shared.NextDouble() * ErMath.TAU;
            var dir = ErVec2.FromAngle(angle) * 128;
            var pos = dir + Knight.Position;
            if(!Knight.CanSeePoint(pos)) return false;
            if(!SwGame.GetMap().TryGetRoom(pos, out var targetRoom)) return false;
            if(!SwGame.GetMap().TryGetRoom(Knight.Position, out var room)) return false;
            if(targetRoom.Id != room.Id) return false;
            Knight.TargetPosition = pos;
            return true;
        }
        private void SetNewWander()
        {
            Knight.TimeoutClock = 4;
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
            if(Knight.CanSeePlayer())StateMachine.Value.SetState("chasing");
            else if(NeedsNewTarget()) SetNewWander();
            else Knight.TimeoutClock -= SwGame.DeltaTime;
            Knight.MoveToTarget(Knight.BaseSpeed * Knight.WanderSpeedMul);
            PlayBodyAnim(2);
        }
    }
    private class Knockback(SwKnight parent) : SwKnightState(parent)
    {
        public override string Name => "knockback";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            BodySprite.Value.Play("death");
            BodySprite.Value.Stop();
        }
        public override void Update()
        {
            base.Update();
            double speed = Knight.Velocity.GetLength();
            if(speed > ErMath.EPSILON) Knight.Velocity = Knight.Velocity.Normalized() * speed * 0.95;
            if(Knight.IsKnockback) return;
            if(Knight.IsAlive) StateMachine.Value.SetState(Knight.IsPassive ? "default" : "wandering");
            else StateMachine.Value.SetState("dead");
        }
    }
    private class Chasing(SwKnight parent) : SwKnightState(parent)
    {
        public override string Name => "chasing";
        private void StateChange()
        {
            if (!Knight.CanSeePlayer())
            {
                StateMachine.Value.SetState("seeking");
                return;
            }
            double attackRange = 64;
            Knight.TargetPosition = SwGame.PlayerPos;
            if(Knight.DistanceToTarget() < attackRange) StateMachine.Value.SetState("attacking");
            Knight.MoveToTarget(Knight.BaseSpeed);
        }
        public override void Update()
        {
            base.Update();
            if (!Knight.CanSeePlayer())
            {
                StateMachine.Value.SetState("seeking");
                return;
            }
            double attackRange = 64;
            Knight.TargetPosition = SwGame.PlayerPos;
            if(Knight.DistanceToTarget() < attackRange) StateMachine.Value.SetState("attacking");
            Knight.MoveToTarget(Knight.BaseSpeed);
            PlayBodyAnim();
        }
    }
    private class Seeking(SwKnight parent) : SwKnightState(parent)
    {
        public override string Name => "seeking";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Knight.TimeoutClock = 4;
        }
        public override void Update()
        {
            base.Update();
            if(NeedsNewTarget()) StateMachine.Value.SetState("wandering");
            else Knight.MoveToTarget(Knight.BaseSpeed);
            PlayBodyAnim();
        }
    }
    private class Attacking(SwKnight parent) : SwKnightState(parent)
    {
        public override string Name => "attacking";
        private ErRect2 GetHurtbox()
        {
            var dir = ErVec2.FromAngle(Knight.FacingIdx * ErMath.HALF_PI);
            double dis = 32;
            ErVec2 size = new(32, 32);
            var pos = Parent.Position + dir * dis;
            return ErRect2.Centered(pos, size);
        }
        private void Attack()
        {
            double attackDuration = 0.125 * 7;
            Knight.TimeoutClock = attackDuration;
            SwordSprite.Value.Visible = true;
            // Note: this resets the frame to 0
            SwordSprite.Value.Stop();
            SwordSprite.Value.Play();
            SwordSprite.Value.Angle = (Knight.FacingIdx - 1) * ErMath.HALF_PI;
            // do damage
            SwDamage damage = new(10, Knight.Position);
            SwGame.EnqueueCommandRect(2, GetHurtbox(), new("damage", damage.ToPri()));
        }
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Knight.Velocity = ErVec2.Zero;
            Attack();
        }
        public override void Update()
        {
            base.Update();
            if(!SwordSprite.Value.IsPlaying) SwordSprite.Value.Visible = false;
            if(Knight.TimeoutClock <= 0) StateMachine.Value.SetState("chasing");
            else Knight.TimeoutClock -= SwGame.DeltaTime;
            PlayBodyAnim();
        }
        public override void EndState(string nextState)
        {
            base.EndState(nextState);
            SwordSprite.Value.Visible = false;
        }
    }
    private class Dead(SwKnight parent) : SwKnightState(parent)
    {
        public override string Name => "dead";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Knight.Velocity = ErVec2.Zero;
            BodySprite.Value.Play("death");
        }
        public override void Update()
        {
            base.Update();
        }
    }
    public static SwStateMachine GetStateMachine(SwKnight parent, string name)
    {
        return new(parent, name, [
            // default
            new Default(parent),
            // wandering
            new Wandering(parent),
            // chasing
            new Chasing(parent),
            // seeking
            new Seeking(parent),
            // attacking
            new Attacking(parent),
            // knockback
            new Knockback(parent),
            // dead
            new Dead(parent),
        ]);
    }
}