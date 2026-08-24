using Eris;
using Eris.Utils;
using ErisMath;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Game.Entity.Projectile;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.Actor.Player;

public abstract class SwPlayerState : SwEntState<SwPlayer>
{
    private SwSprite BodySprite = null!;
    private SwSprite HatSprite = null!;
    private SwSprite SpoonSprite = null!;
    private SwSprite SlingSprite = null!;
    private SwSprite ReticleSprite = null!;
    private SwPlayerControls Controls = null!;
    private SwAreaComponent SpoonHurtbox = null!;
    // name, hands, facing
    private static readonly string[][][] BodyAnims = [
        [
            [
                "idle_dr_0h",
                "idle_d_0h",
                "idle_dl_0h",
                "idle_u_0h",
            ],
            [
                "idle_dr_1h",
                "idle_d_1h",
                "idle_dl_1h",
                "idle_u_1h",
            ],
            [
                "idle_dr_2h",
                "idle_d_2h",
                "idle_dl_2h",
                "idle_u_2h",
            ],
        ],
        [
            [
                "move_dr_0h",
                "move_d_0h",
                "move_dl_0h",
                "move_u_0h",
            ],
            [
                "move_dr_1h",
                "move_d_1h",
                "move_dl_1h",
                "move_u_1h",
            ],
            [
                "move_dr_2h",
                "move_d_2h",
                "move_dl_2h",
                "move_u_2h",
            ],
        ],
    ];
    public string[] DodgeAnims = [
        "def_dodge_dr",
        "def_dodge_d",
        "def_dodge_dl",
        "def_dodge_u",
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
    private bool CanAttack()
    {
        if(Entity.AttackCooldownClock > 0) return false;
        return true;
    }
    public override void Init(SwStateMachine stateMachine)
    {
        base.Init(stateMachine);
        BodySprite = Entity.GetComponent<SwSpriteComponent>("body")?.Sprite!;
        HatSprite = Entity.GetComponent<SwSpriteComponent>("hat")?.Sprite!;
        SpoonSprite = Entity.GetComponent<SwSpriteComponent>("spoon")?.Sprite!;
        SlingSprite = Entity.GetComponent<SwSpriteComponent>("sling")?.Sprite!;
        ReticleSprite = Entity.GetComponent<SwSpriteComponent>("reticle")?.Sprite!;
        Controls = Entity.GetComponent<SwPlayerControls>("controls")!;
        SpoonHurtbox = Entity.GetComponent<SwAreaComponent>("spoon_hurtbox")!;
    }
    private void SetBodyHandedAnim(int animIdx, int hands, int facing)
    {
        string animName = BodyAnims[animIdx][hands][facing];
        BodySprite.Play(animName);
        HatSprite.Play(animName);
    }
    private void SetBodyDodgeAnim(int facing)
    {
        string animName = DodgeAnims[Controls.LastFacingIdx];
        BodySprite.Play(animName);
        HatSprite.Play(animName);
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
            SetBodyHandedAnim(animIdx, 2, Controls.LastFacingIdx);
            Entity.Velocity = Controls.Move * Entity.BaseSpeed;
            if(Controls.AttackJustPressed && CanAttack()) StateMachine.SetState("attack");
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
            // BodySprite.Play(BodyAnims[0][0][Controls.LastFacingIdx]);
            SetBodyHandedAnim(0, 0, Controls.LastFacingIdx);
            Entity.Velocity = ErVec2.Zero;
            SetHurtbox();
            // SpoonHurtbox.Enabled = true;
            // SwDamage damage = new(10, Entity.Position);
            // SwGame.EnqueueCommandRect(4, GetHurtbox(), damage.ToPri());
            // BodySprite.SetPallet(1);
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
            // BodySprite.SetPallet(0);
            SpoonHurtbox.Enabled = false;
        }
        private void SetHurtbox()
        {
            var dir = ErVec2.FromAngle(Controls.LastFacingIdx * ErMath.HALF_PI);
            double dis = 32;
            SpoonHurtbox.Offset = dir * dis;
            SpoonHurtbox.Enabled = true;
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
            
            // BodySprite.Play(BodyAnims[animIdx][1][Controls.LastFacingIdx]);
            SetBodyHandedAnim(animIdx, 1, Controls.LastFacingIdx);
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
            if(SwGame.ParticleEmitters.TryGetValue(Entity.Id, out var emitter))
            {
                emitter.Emitting = true;
            }
        }
        private bool CanFire()
        {
            if(!Controls.FireJustPressed) return false;
            if(!Controls.Aim.IsNonzero()) return false;
            return true;
        }
        public override void Update()
        {
            base.Update();
            int animIdx = Entity.Velocity.IsNonzero() ? 1 : 0;
            // BodySprite.Play(BodyAnims[animIdx][1][Controls.LastFacingIdx]);
            SetBodyHandedAnim(animIdx, 1, Controls.LastFacingIdx);
            Entity.Velocity = Controls.Move * Entity.BaseSpeed * Entity.ChargeSpeedMul;
            if (!Controls.IsCharging) StateMachine.SetState("default");
            else if (CanFire())
            {
                // fire!
                var pos = Entity.Position;// - Entity.Size * 0.5;
                Entity.EntProps.Props.TrySet("bullet/x", pos.X);
                Entity.EntProps.Props.TrySet("bullet/y", pos.Y);
                Entity.EntProps.Props.TrySet("bullet/x_velocity", Controls.Aim.X * Entity.BulletSpeed);
                Entity.EntProps.Props.TrySet("bullet/y_velocity", Controls.Aim.Y * Entity.BulletSpeed);
                SwGame.Game.AddEntity<SwProjectile>(Entity.EntProps.Props.Get("bullet"));
                StateMachine.SetState("default");
                Entity.AttackCooldownClock = 0.1;
            }
        }
        public override void EndState(string nextState)
        {
            base.EndState(nextState);
            SlingSprite.Visible = false;
            SlingSprite.Stop();
            ReticleSprite.Play("still");
            if(SwGame.ParticleEmitters.TryGetValue(Entity.Id, out var emitter))
            {
                emitter.Emitting = false;
            }
        }
    }
    public class Dodging: SwPlayerState
    {
        public override string Name => "dodging";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            BodySprite.Stop();
            SetBodyDodgeAnim(Controls.LastFacingIdx);
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