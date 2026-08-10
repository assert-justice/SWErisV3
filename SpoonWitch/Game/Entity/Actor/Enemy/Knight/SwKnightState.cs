using Eris;
using Eris.Utils;
using ErisMath;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Knight;

public abstract class SwKnightState: SwEntState<SwKnight>
{
    private SwSprite BodySprite = null!;
    private SwSprite SwordSprite = null!;
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
    public override void Init(SwStateMachine stateMachine)
    {
        base.Init(stateMachine);
        BodySprite = Entity.GetComponent<SwSpriteComponent>("body")?.Sprite!;
        SwordSprite = Entity.GetComponent<SwSpriteComponent>("sword")?.Sprite!;
    }
    private void PlayBodyAnim(int hands, byte facing)
    {
        BodySprite.Play(BodyAnims[hands][facing]);
    }
    private void PlayBodyAnim(int hands = 2)
    {
        PlayBodyAnim(hands, Entity.FacingIdx);
    }
    private bool NeedsNewTarget()
    {
        if(Entity.TimeoutClock <= 0) return true;
        if(Entity.Velocity.GetLengthSquared() < CLOSE_ENOUGH) return true;
        if(Entity.DistanceToTarget() < CLOSE_ENOUGH) return true;
        return false;
    }
    // public override void BeginState(string lastState)
    // {
    //     base.BeginState(lastState);
    //     ErEngine.Log(Name);
    // }
    private class Default: SwKnightState
    {
        public override string Name => "default";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Entity.Velocity = ErVec2.Zero;
        }
        public override void Update()
        {
            base.Update();
            BodySprite.Play("move_2h_d");
        }
    }
    private class Wandering: SwKnightState
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
            return true;
        }
        private void SetNewWander()
        {
            Entity.TimeoutClock = 4;
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
            else if(NeedsNewTarget()) SetNewWander();
            else Entity.TimeoutClock -= SwGame.DeltaTime;
            Entity.MoveToTarget(Entity.BaseSpeed * Entity.WanderSpeedMul);
            PlayBodyAnim(2);
        }
    }
    private class Knockback: SwKnightState
    {
        public override string Name => "knockback";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
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
    private class Chasing: SwKnightState
    {
        public override string Name => "chasing";
        public override void Update()
        {
            base.Update();
            if (!Entity.CanSeePlayer())
            {
                StateMachine.SetState("seeking");
                return;
            }
            double attackRange = 64;
            Entity.TargetPosition = SwGame.PlayerPos;
            if(Entity.DistanceToTarget() < attackRange) StateMachine.SetState("attacking");
            Entity.MoveToTarget(Entity.BaseSpeed);
            PlayBodyAnim();
        }
    }
    private class Seeking: SwKnightState
    {
        public override string Name => "seeking";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Entity.TimeoutClock = 4;
        }
        public override void Update()
        {
            base.Update();
            if(NeedsNewTarget()) StateMachine.SetState("wandering");
            else Entity.MoveToTarget(Entity.BaseSpeed);
            PlayBodyAnim();
        }
    }
    private class Attacking: SwKnightState
    {
        public override string Name => "attacking";
        private ErRect2 GetHurtbox()
        {
            var dir = ErVec2.FromAngle(Entity.FacingIdx * ErMath.HALF_PI);
            double dis = 32;
            ErVec2 size = new(32, 32);
            var pos = Parent.Position + dir * dis;
            return ErRect2.Centered(pos, size);
        }
        private void Attack()
        {
            double attackDuration = 0.125 * 7;
            Entity.TimeoutClock = attackDuration;
            SwordSprite.Visible = true;
            SwordSprite.Play();
            SwordSprite.Angle = (Entity.FacingIdx - 1) * ErMath.HALF_PI;
            // do damage
            SwDamage damage = new(10, Entity.Position);
            SwGame.EnqueueCommandRect(2, GetHurtbox(), damage.ToPri());
        }
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Entity.Velocity = ErVec2.Zero;
            Attack();
        }
        public override void Update()
        {
            base.Update();
            if(!SwordSprite.IsPlaying) SwordSprite.Visible = false;
            if(Entity.TimeoutClock <= 0) StateMachine.SetState("chasing");
            else Entity.TimeoutClock -= SwGame.DeltaTime;
            PlayBodyAnim();
        }
        public override void EndState(string nextState)
        {
            base.EndState(nextState);
            SwordSprite.Visible = false;
        }
    }
    private class Dead: SwKnightState
    {
        public override string Name => "dead";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Entity.Velocity = ErVec2.Zero;
            BodySprite.Play("death");
        }
        public override void Update()
        {
            base.Update();
        }
    }
    public static SwStateMachine GetStateMachine(SwKnight parent, string name)
    {
        return new(parent, name, [
            new Default(),
            new Wandering(),
            new Chasing(),
            new Seeking(),
            new Attacking(),
            new Knockback(),
            new Dead(),
        ]);
    }
}