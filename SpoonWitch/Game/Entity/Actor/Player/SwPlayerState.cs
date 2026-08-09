using Eris;
using Eris.Utils;
using ErisMath;
using SpoonWitch.Game.Entity.Component.Sprite;
using SpoonWitch.Game.Entity.Component.State;

namespace SpoonWitch.Game.Entity.Actor.Player;

public abstract class SwPlayerState(SwPlayer parent) : SwState(parent)
{
    protected readonly SwPlayer Player = parent;
    protected ErWrapper<SwSprite> BodySprite = new(() => parent.GetComponent<SwSprite>("body")!);
    protected ErWrapper<SwSprite> SpoonSprite = new(() => parent.GetComponent<SwSprite>("spoon")!);
    protected ErWrapper<SwSprite> SlingSprite = new(() => parent.GetComponent<SwSprite>("sling")!);
    protected ErWrapper<SwSprite> ReticleSprite = new(() => parent.GetComponent<SwSprite>("reticle")!);
    protected ErWrapper<SwStateMachine> StateMachine = new(() => parent.GetComponent<SwStateMachine>("state_machine")!);
    protected ErWrapper<SwPlayerControls> Controls = new(() => parent.GetComponent<SwPlayerControls>("controls")!);
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
        if(!Controls.Value.Move.IsNonzero()) return false;
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
        ReticleSprite.Value.Visible = Controls.Value.ReticleVisible;
        ReticleSprite.Value.Offset = Controls.Value.ReticlePosition;
    }
    public class Default(SwPlayer parent) : SwPlayerState(parent)
    {
        public override string Name => "default";
        public override void Update()
        {
            base.Update();
            int animIdx = Player.Velocity.IsNonzero() ? 1 : 0;
            BodySprite.Value.Play(BodyAnims[animIdx][2][Controls.Value.LastFacingIdx]);
            Player.Velocity = Controls.Value.Move * Player.BaseSpeed;
            if(Controls.Value.AttackJustPressed) StateMachine.Value.SetState("attack");
            else if(Controls.Value.IsCharging) StateMachine.Value.SetState("charging");
            else if(Controls.Value.DodgeJustPressed && CanDodge()) StateMachine.Value.SetState("dodging");
        }
    }
    public class Attack(SwPlayer parent) : SwPlayerState(parent)
    {
        public override string Name => "attack";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            SpoonSprite.Value.Visible = true;
            SpoonSprite.Value.Angle = (Controls.Value.LastFacingIdx - 1) * ErMath.HALF_PI;
            SpoonSprite.Value.Play();
            BodySprite.Value.Play(BodyAnims[0][0][Controls.Value.LastFacingIdx]);
            Player.Velocity = ErVec2.Zero;
            SwDamage damage = new(10, Player.Position);
            SwGame.EnqueueCommandRect(4, GetHurtbox(), new("damage", damage.ToPri()));
        }
        public override void Update()
        {
            base.Update();
            if(!SpoonSprite.Value.IsPlaying) StateMachine.Value.SetState("default");
        }
        public override void EndState(string nextState)
        {
            base.EndState(nextState);
            SpoonSprite.Value.Visible = false;
        }
        private ErRect2 GetHurtbox()
        {
            var dir = Controls.Value.LastFacing;
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
            SlingSprite.Value.Visible = true;
            SlingSprite.Value.Play("charging");
            ReticleSprite.Value.Play(ReticleAnims[0]);
            Player.Clock0 = 0;
        }
        public override void Update()
        {
            base.Update();
            int animIdx = Player.Velocity.IsNonzero() ? 1 : 0;
            BodySprite.Value.Play(BodyAnims[animIdx][1][Controls.Value.LastFacingIdx]);
            Player.Velocity = Controls.Value.Move * Player.BaseSpeed * Player.ChargeSpeedMul;
            if (!Controls.Value.IsCharging)
            {
                SlingSprite.Value.Visible = false;
                SlingSprite.Value.Stop();
                ReticleSprite.Value.Play("still");
                StateMachine.Value.SetState("default");
                return;
            }
            int lastThresh = ErMath.FloorToInt(Player.Clock0 * 3 / Player.ChargeTime);
            Player.Clock0 += SwGame.DeltaTime;
            int nextThresh = ErMath.FloorToInt(Player.Clock0 * 3 / Player.ChargeTime);
            if(lastThresh == nextThresh) return;
            int frame = ReticleSprite.Value.FrameIdx;
            double progress = ReticleSprite.Value.FrameProgress;
            ReticleSprite.Value.Play(ReticleAnims[nextThresh]);
            ReticleSprite.Value.FrameIdx = frame;
            ReticleSprite.Value.FrameProgress = progress;
            if(nextThresh == 3) StateMachine.Value.SetState("charged");
        }
    }
    public class Charged(SwPlayer parent) : SwPlayerState(parent)
    {
        public override string Name => "charged";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            SlingSprite.Value.Play("charged");
        }
        public override void Update()
        {
            base.Update();
            int animIdx = Player.Velocity.IsNonzero() ? 1 : 0;
            BodySprite.Value.Play(BodyAnims[animIdx][1][Controls.Value.LastFacingIdx]);
            Player.Velocity = Controls.Value.Move * Player.BaseSpeed * Player.ChargeSpeedMul;
            if (!Controls.Value.IsCharging) StateMachine.Value.SetState("default");
        }
        public override void EndState(string nextState)
        {
            base.EndState(nextState);
            SlingSprite.Value.Visible = false;
            SlingSprite.Value.Stop();
            ReticleSprite.Value.Play("still");
        }
    }
    public class Dodging(SwPlayer parent) : SwPlayerState(parent)
    {
        public override string Name => "dodging";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            BodySprite.Value.Stop();
            BodySprite.Value.Play(DodgeAnims[Controls.Value.LastFacingIdx]);
            Player.Clock0 = 0;
            // set and lock in velocity
            Player.Velocity = Controls.Value.Move * Player.BaseSpeed * Player.DodgeSpeedMul;
            // ErEngine.Log("start dodge");
        }
        public override void Update()
        {
            base.Update();
            double elapsed = Player.Clock0;
            Player.Clock0 += SwGame.DeltaTime;
            if(Player.Clock0 > Player.DodgeDuration) StateMachine.Value.SetState("default");
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