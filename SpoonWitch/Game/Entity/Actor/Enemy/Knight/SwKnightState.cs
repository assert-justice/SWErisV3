using Eris;
using ErisMath;
using SpoonWitch.Game.Entity.Actor.Player;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Knight;

public abstract class SwKnightState: SwState<SwKnight>
{
    private SwSprite BodySprite = null!;
    private SwSprite SwordSprite = null!;
    private SwAreaComponent Hurtbox = null!;
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
    public override void Ready()
    {
        base.Ready();
        BodySprite = Entity.GetComponent<SwSpriteComponent>("body")?.Sprite!;
        SwordSprite = Entity.GetComponent<SwSpriteComponent>("sword")?.Sprite!;
        Hurtbox = Entity.GetComponent<SwAreaComponent>("hurtbox")!;
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
        if(!Entity.TimeoutClock.IsRunning) return true;
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
        public override void Update(double dt)
        {
            base.Update(dt);
            BodySprite.Play("move_2h_d");
            BodySprite.Stop();
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
            ErEngine.LogWarning("knight could not find target pos");
        }
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            SetNewWander();
        }
        public override void Update(double dt)
        {
            base.Update(dt);
            if(Entity.CanSeeAnyPlayer())StateMachine.SetState("chasing");
            else if(NeedsNewTarget()) SetNewWander();
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
        public override void Update(double dt)
        {
            base.Update(dt);
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
            if(target is null || !Entity.CanSeePoint(target.Position))
            {
                StateMachine.SetState("seeking");
                return;
            }
            double attackRange = 64;
            // Note: we are also setting the target position because if we lose sight of the target, the seek state will use it
            Entity.TargetPosition = target.Position;
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
            Entity.TimeoutClock.Start(4);
        }
        public override void Update(double dt)
        {
            base.Update(dt);
            if(NeedsNewTarget()) StateMachine.SetState("wandering");
            else Entity.MoveToTarget(Entity.BaseSpeed);
            PlayBodyAnim();
        }
    }
    private class Attacking: SwKnightState
    {
        public override string Name => "attacking";
        private enum AttackPhase
        {
            Startup,
            Hurt,
            Cooldown,
        }
        private AttackPhase Phase;
        private void SetHurtbox()
        {
            var dir = ErVec2.FromAngle(Entity.FacingIdx * ErMath.HALF_PI);
            double dis = 32;
            Hurtbox.Offset = dir * dis;
            Hurtbox.Enabled = true;
        }
        private void Attack()
        {
            Entity.TimeoutClock.Start(Entity.HurtDelay);
            SwordSprite.Visible = true;
            SwordSprite.Play();
            SwordSprite.Angle = (Entity.FacingIdx - 1) * ErMath.HALF_PI;
            SwordSprite.HFlip = !SwordSprite.HFlip;
            Phase = AttackPhase.Startup;
        }
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Entity.Velocity = ErVec2.Zero;
            Attack();
            SetHurtbox();
        }
        public override void Update(double dt)
        {
            base.Update(dt);
            if(!SwordSprite.IsPlaying) SwordSprite.Visible = false;
            if(Entity.TimeoutClock.IsRunning) return;
            switch (Phase)
            {
                case AttackPhase.Startup:
                    Entity.TimeoutClock.Start(Entity.HurtDelay);
                    Phase = AttackPhase.Hurt;
                    Hurtbox.Enabled = true;
                    break;
                case AttackPhase.Hurt:
                    Entity.TimeoutClock.Start(Entity.Cooldown);
                    Phase = AttackPhase.Cooldown;
                    Hurtbox.Enabled = false;
                    break;
                case AttackPhase.Cooldown:
                    StateMachine.SetState("chasing");
                    break;
            }
            // Hurtbox.Enabled = SwordSprite.FrameIdx == 0;
            // if(!Entity.TimeoutClock.IsRunning) StateMachine.SetState("chasing");
            // PlayBodyAnim();
        }
        public override void EndState(string nextState)
        {
            base.EndState(nextState);
            SwordSprite.Visible = false;
            Hurtbox.Enabled = false;
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
    }
    public static SwStateMachine<SwKnight> GetStateMachine(SwKnight parent, string name)
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