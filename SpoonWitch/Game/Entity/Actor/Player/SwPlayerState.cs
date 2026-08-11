using Eris;
using Eris.Utils;
using ErisMath;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.Actor.Player;

public abstract class SwPlayerState : SwEntState<SwPlayer>
{
    private SwSprite BodySprite = null!;
    private SwSprite SpoonSprite = null!;
    private SwSprite SlingSprite = null!;
    private SwSprite ReticleSprite = null!;
    private SwPlayerControls Controls = null!;
    // name, hands, facing
    private static readonly string[][][] BodyAnims = [
        [
            [
                "idle_0h_dr",
                "idle_0h_d",
                "idle_0h_dl",
                "idle_0h_u",
            ],
            [
                "idle_1h_dr",
                "idle_1h_d",
                "idle_1h_dl",
                "idle_1h_u",
            ],
            [
                "idle_2h_dr",
                "idle_2h_d",
                "idle_2h_dl",
                "idle_2h_u",
            ],
        ],
        [
            [
                "run_0h_dr",
                "run_0h_d",
                "run_0h_dl",
                "run_0h_u",
            ],
            [
                "run_1h_dr",
                "run_1h_d",
                "run_1h_dl",
                "run_1h_u",
            ],
            [
                "run_2h_dr",
                "run_2h_d",
                "run_2h_dl",
                "run_2h_u",
            ],
        ],
    ];
    public string[] DodgeAnims = [
        "dodge_dr",
        "dodge_d",
        "dodge_dl",
        "dodge_u",
    ];
    private static readonly string[] ReticleAnims = [
        "charge_0",
        "charge_1",
        "charge_2",
        "charge_3",
    ];
    private bool CanDodge()
    {
        if(Entity.DodgeCooldownClock > 0) return false;
        if(!Controls.Move.IsNonzero()) return false;
        return true;
    }
    public override void Init(SwStateMachine stateMachine)
    {
        base.Init(stateMachine);
        BodySprite = Entity.GetComponent<SwSpriteComponent>("body")?.Sprite!;
        SpoonSprite = Entity.GetComponent<SwSpriteComponent>("spoon")?.Sprite!;
        SlingSprite = Entity.GetComponent<SwSpriteComponent>("sling")?.Sprite!;
        ReticleSprite = Entity.GetComponent<SwSpriteComponent>("reticle")?.Sprite!;
        Controls = Entity.GetComponent<SwPlayerControls>("controls")!;
    }
    // public override void BeginState(string lastState)
    // {
    //     base.BeginState(lastState);
    //     ErEngine.Log(Name);
    // }
    public override void Update()
    {
        base.Update();
        ReticleSprite.Visible = Controls.ReticleVisible;
        ReticleSprite.Offset = Controls.ReticlePosition;
    }
    public class Default: SwPlayerState
    {
        public override string Name => "default";
        public override void Update()
        {
            base.Update();
            int animIdx = Entity.Velocity.IsNonzero() ? 1 : 0;
            BodySprite.Play(BodyAnims[animIdx][2][Controls.LastFacingIdx]);
            Entity.Velocity = Controls.Move * Entity.BaseSpeed;
            if(Controls.AttackJustPressed) StateMachine.SetState("attack");
            else if(Controls.IsCharging) StateMachine.SetState("charging");
            else if(Controls.DodgeJustPressed && CanDodge()) StateMachine.SetState("dodging");
        }
    }
    public class Attack: SwPlayerState
    {
        public override string Name => "attack";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            SpoonSprite.Visible = true;
            SpoonSprite.Angle = (Controls.LastFacingIdx - 1) * ErMath.HALF_PI;
            SpoonSprite.Play();
            BodySprite.Play(BodyAnims[0][0][Controls.LastFacingIdx]);
            Entity.Velocity = ErVec2.Zero;
            SwDamage damage = new(10, Entity.Position);
            SwGame.EnqueueCommandRect(4, GetHurtbox(), damage.ToPri());
            BodySprite.SetPallet(1);
        }
        public override void Update()
        {
            base.Update();
            if(!SpoonSprite.IsPlaying) StateMachine.SetState("default");
        }
        public override void EndState(string nextState)
        {
            base.EndState(nextState);
            SpoonSprite.Visible = false;
            BodySprite.SetPallet(0);
        }
        private ErRect2 GetHurtbox()
        {
            var dir = Controls.LastFacing;
            double dis = 32;
            ErVec2 size = new(32, 32);
            var pos = Parent.Position + dir * dis;
            return ErRect2.Centered(pos, size);
        }
    }
    public class Charging: SwPlayerState
    {
        public override string Name => "charging";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            SlingSprite.Visible = true;
            SlingSprite.Play("charging");
            ReticleSprite.Play(ReticleAnims[0]);
            Entity.Clock0 = 0;
        }
        public override void Update()
        {
            base.Update();
            int animIdx = Entity.Velocity.IsNonzero() ? 1 : 0;
            BodySprite.Play(BodyAnims[animIdx][1][Controls.LastFacingIdx]);
            Entity.Velocity = Controls.Move * Entity.BaseSpeed * Entity.ChargeSpeedMul;
            if (!Controls.IsCharging)
            {
                SlingSprite.Visible = false;
                SlingSprite.Stop();
                ReticleSprite.Play("still");
                StateMachine.SetState("default");
                return;
            }
            int lastThresh = ErMath.FloorToInt(Entity.Clock0 * 3 / Entity.ChargeTime);
            Entity.Clock0 += SwGame.DeltaTime;
            int nextThresh = ErMath.FloorToInt(Entity.Clock0 * 3 / Entity.ChargeTime);
            if(lastThresh == nextThresh) return;
            int frame = ReticleSprite.FrameIdx;
            double progress = ReticleSprite.FrameProgress;
            ReticleSprite.Play(ReticleAnims[nextThresh]);
            ReticleSprite.FrameIdx = frame;
            ReticleSprite.FrameProgress = progress;
            // ErEngine.Log("charge level ", nextThresh);
            if(nextThresh == 3) StateMachine.SetState("charged");
        }
    }
    public class Charged: SwPlayerState
    {
        public override string Name => "charged";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            SlingSprite.Play("charged");
        }
        public override void Update()
        {
            base.Update();
            int animIdx = Entity.Velocity.IsNonzero() ? 1 : 0;
            BodySprite.Play(BodyAnims[animIdx][1][Controls.LastFacingIdx]);
            Entity.Velocity = Controls.Move * Entity.BaseSpeed * Entity.ChargeSpeedMul;
            if (!Controls.IsCharging) StateMachine.SetState("default");
        }
        public override void EndState(string nextState)
        {
            base.EndState(nextState);
            SlingSprite.Visible = false;
            SlingSprite.Stop();
            ReticleSprite.Play("still");
        }
    }
    public class Dodging: SwPlayerState
    {
        public override string Name => "dodging";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            BodySprite.Stop();
            BodySprite.Play(DodgeAnims[Controls.LastFacingIdx]);
            Entity.Clock0 = 0;
            // set and lock in velocity
            Entity.Velocity = Controls.Move * Entity.BaseSpeed * Entity.DodgeSpeedMul;
            // ErEngine.Log("start dodge");
        }
        public override void Update()
        {
            base.Update();
            double elapsed = Entity.Clock0;
            Entity.Clock0 += SwGame.DeltaTime;
            if(Entity.Clock0 > Entity.DodgeDuration) StateMachine.SetState("default");
            // Note: edge detection. fires when the clock is now past invuln delay
            else if(Entity.Clock0 >= Entity.DodgeInvulnDelay && elapsed < Entity.DodgeInvulnDelay) Entity.InvulnClock = Entity.DodgeInvulnWindow;
        }
        public override void EndState(string nextState)
        {
            base.EndState(nextState);
            Entity.DodgeCooldownClock = Entity.DodgeCooldown;
        }
    }
    public static SwStateMachine GetStateMachine(SwPlayer parent, string name)
    {
        return new(parent, name, [
            new Default(),
            new Attack(),
            new Charging(),
            new Charged(),
            new Dodging(),
        ]);
    }
}