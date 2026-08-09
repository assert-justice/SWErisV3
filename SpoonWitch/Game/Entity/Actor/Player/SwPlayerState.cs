using Eris;
using Eris.Utils;
using ErisMath;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.Actor.Player;

public abstract class SwPlayerState(SwPlayer parent) : SwState(parent)
{
    protected readonly SwPlayer Player = parent;
    protected ErWrapper<SwSpriteComponent> _BodySprite = new(() => parent.GetComponent<SwSpriteComponent>("body")!);
    private SwSprite BodySprite => _BodySprite.Value.Sprite;
    protected ErWrapper<SwSpriteComponent> _SpoonSprite = new(() => parent.GetComponent<SwSpriteComponent>("spoon")!);
    private SwSprite SpoonSprite => _SpoonSprite.Value.Sprite;
    protected ErWrapper<SwSpriteComponent> _SlingSprite = new(() => parent.GetComponent<SwSpriteComponent>("sling")!);
    private SwSprite SlingSprite => _SlingSprite.Value.Sprite;
    protected ErWrapper<SwSpriteComponent> _ReticleSprite = new(() => parent.GetComponent<SwSpriteComponent>("reticle")!);
    private SwSprite ReticleSprite => _ReticleSprite.Value.Sprite;
    protected ErWrapper<SwStateMachine> _StateMachine = new(() => parent.GetComponent<SwStateMachine>("state_machine")!);
    private SwStateMachine StateMachine => _StateMachine.Value;
    protected ErWrapper<SwPlayerControls> _Controls = new(() => parent.GetComponent<SwPlayerControls>("controls")!);
    private SwPlayerControls Controls => _Controls.Value;
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
        if(Player.DodgeCooldownClock > 0) return false;
        if(!Controls.Move.IsNonzero()) return false;
        return true;
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
    public class Default(SwPlayer parent) : SwPlayerState(parent)
    {
        public override string Name => "default";
        public override void Update()
        {
            base.Update();
            int animIdx = Player.Velocity.IsNonzero() ? 1 : 0;
            BodySprite.Play(BodyAnims[animIdx][2][Controls.LastFacingIdx]);
            Player.Velocity = Controls.Move * Player.BaseSpeed;
            if(Controls.AttackJustPressed) StateMachine.SetState("attack");
            else if(Controls.IsCharging) StateMachine.SetState("charging");
            else if(Controls.DodgeJustPressed && CanDodge()) StateMachine.SetState("dodging");
        }
    }
    public class Attack(SwPlayer parent) : SwPlayerState(parent)
    {
        public override string Name => "attack";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            SpoonSprite.Visible = true;
            SpoonSprite.Angle = (Controls.LastFacingIdx - 1) * ErMath.HALF_PI;
            SpoonSprite.Play();
            BodySprite.Play(BodyAnims[0][0][Controls.LastFacingIdx]);
            Player.Velocity = ErVec2.Zero;
            SwDamage damage = new(10, Player.Position);
            SwGame.EnqueueCommandRect(4, GetHurtbox(), new("damage", damage.ToPri()));
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
    public class Charging(SwPlayer parent) : SwPlayerState(parent)
    {
        public override string Name => "charging";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            SlingSprite.Visible = true;
            SlingSprite.Play("charging");
            ReticleSprite.Play(ReticleAnims[0]);
            Player.Clock0 = 0;
        }
        public override void Update()
        {
            base.Update();
            int animIdx = Player.Velocity.IsNonzero() ? 1 : 0;
            BodySprite.Play(BodyAnims[animIdx][1][Controls.LastFacingIdx]);
            Player.Velocity = Controls.Move * Player.BaseSpeed * Player.ChargeSpeedMul;
            if (!Controls.IsCharging)
            {
                SlingSprite.Visible = false;
                SlingSprite.Stop();
                ReticleSprite.Play("still");
                StateMachine.SetState("default");
                return;
            }
            int lastThresh = ErMath.FloorToInt(Player.Clock0 * 3 / Player.ChargeTime);
            Player.Clock0 += SwGame.DeltaTime;
            int nextThresh = ErMath.FloorToInt(Player.Clock0 * 3 / Player.ChargeTime);
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
    public class Charged(SwPlayer parent) : SwPlayerState(parent)
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
            int animIdx = Player.Velocity.IsNonzero() ? 1 : 0;
            BodySprite.Play(BodyAnims[animIdx][1][Controls.LastFacingIdx]);
            Player.Velocity = Controls.Move * Player.BaseSpeed * Player.ChargeSpeedMul;
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
    public class Dodging(SwPlayer parent) : SwPlayerState(parent)
    {
        public override string Name => "dodging";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            BodySprite.Stop();
            BodySprite.Play(DodgeAnims[Controls.LastFacingIdx]);
            Player.Clock0 = 0;
            // set and lock in velocity
            Player.Velocity = Controls.Move * Player.BaseSpeed * Player.DodgeSpeedMul;
            // ErEngine.Log("start dodge");
        }
        public override void Update()
        {
            base.Update();
            double elapsed = Player.Clock0;
            Player.Clock0 += SwGame.DeltaTime;
            if(Player.Clock0 > Player.DodgeDuration) StateMachine.SetState("default");
            // Note: edge detection. fires when the clock is now past invuln delay
            else if(Player.Clock0 >= Player.DodgeInvulnDelay && elapsed < Player.DodgeInvulnDelay) Player.InvulnClock = Player.DodgeInvulnWindow;
        }
        public override void EndState(string nextState)
        {
            base.EndState(nextState);
            Player.DodgeCooldownClock = Player.DodgeCooldown;
        }
    }
    public static SwStateMachine GetStateMachine(SwPlayer parent, string name)
    {
        return new(parent, name, [
            new Default(parent),
            new Attack(parent),
            new Charging(parent),
            new Charged(parent),
            new Dodging(parent),
        ]);
    }
}