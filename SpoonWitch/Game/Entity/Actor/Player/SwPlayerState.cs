using Eris;
using Eris.Utils;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Game.Entity.Projectile;
using SpoonWitch.Game.Inventory;
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
    private SwParticleComponent DustParticles = null!;
    private SwInventoryComponent _Inventory = null!;
    private SwInventory Inventory => _Inventory.Entries!;
    protected virtual double StaminaRegenClockMul => 1;
    protected virtual double ManaRegenMul => 1;
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
        if(Entity.Stamina <= 0) return false;
        return true;
    }
    private bool CanAttack()
    {
        if(Entity.AttackCooldownClock > 0) return false;
        if(Entity.Stamina <= 0) return false;
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
        DustParticles = Entity.GetComponent<SwParticleComponent>("dust_1")!;
        _Inventory = Entity.GetComponent<SwInventoryComponent>("inventory")!;
    }
    private void SetBodyHandedAnim(int animIdx, int hands, int facing)
    {
        string animName = BodyAnims[animIdx][hands][facing];
        BodySprite.Play(animName);
        HatSprite.Play(animName);
    }
    private void SetBodyDodgeAnim(int facing)
    {
        string animName = DodgeAnims[facing];
        BodySprite.Play(animName);
        HatSprite.Play(animName);
    }
    private void PlayBodyAnim(string animName)
    {
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
        if(Entity.Stamina < Entity.MaxStamina)
        {
            if(Entity.StaminaRegenClock > 0) Entity.StaminaRegenClock -= SwGame.DeltaTime * StaminaRegenClockMul;
            else
            {
                Entity.Stamina += Entity.StaminaRegen * SwGame.DeltaTime;
                if(Entity.Stamina > Entity.MaxStamina) Entity.Stamina = Entity.MaxStamina;
            }
        }
        if(Entity.Mana < Entity.MaxMana)
        {
            Entity.Mana += Entity.ManaRegen * SwGame.DeltaTime * ManaRegenMul;
            if(Entity.Mana > Entity.MaxMana) Entity.Mana = Entity.MaxMana;
        }
    }
    public class Dead: SwPlayerState
    {
        public override string Name => "dead";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            PlayBodyAnim("die");
            Entity.Velocity = ErVec2.Zero;
        }
        public override void Update()
        {
            base.Update();
            if(BodySprite.IsPlaying) return;
            if(BodySprite.CurrentAnimation.Name == "die") PlayBodyAnim("continue");
            else if(SwGame.Map.InSameRoom(Entity.Position, SwGame.ActiveCheckpoint.RectPx.Center)) StateMachine.SetState("respawn");
            else StateMachine.SetState("respawn_fade_out");
        }
    }
    public class RespawnFadeOut: SwPlayerState
    {
        public override string Name => "respawn_fade_out";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            PlayBodyAnim("fly");
            SwGame.Game.FadeOut();
        }
        public override void Update()
        {
            base.Update();
            bool isVisible = SwGame.Camera.IsPointVisible(Entity.Position);
            if (isVisible)
            {
                Entity.MoveToward(SwGame.ActiveCheckpoint.RectPx.Center, Entity.BaseSpeed);
            }
            else Entity.Velocity = ErVec2.Zero;
            if(SwGame.Game.FadeState == 1 && !isVisible) StateMachine.SetState("respawn_fade_in");
        }
    }
    public class RespawnFadeIn: SwPlayerState
    {
        public override string Name => "respawn_fade_in";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            PlayBodyAnim("fly");
            SwGame.Game.FadeIn();
            SwGame.SetCameraTarget(SwGame.ActiveCheckpoint.RectPx.Center, true);
            ErVec2 diff = Entity.Position - SwGame.ActiveCheckpoint.RectPx.Center;
            double distance = diff.GetLength();
            if(500 < distance) distance = 500;
            ErVec2 offset = diff.Normalized() * distance;
            ErVec2 pos = SwGame.ActiveCheckpoint.RectPx.Center + offset;
            Entity.Position = pos;
        }
        public override void Update()
        {
            base.Update();
            if(BodySprite.CurrentAnimation.Name == "respawn")
            {
                if (!BodySprite.IsPlaying)
                {
                    StateMachine.SetState("default");
                    Entity.IsAlive = true;
                }
                return;
            }
            double distance = Entity.MoveToward(SwGame.ActiveCheckpoint.RectPx.Center, Entity.BaseSpeed);
            if(distance == 0) PlayBodyAnim("respawn");
        }
    }
    public class Respawn: SwPlayerState
    {
        public override string Name => "respawn";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            PlayBodyAnim("fly");
        }
        public override void Update()
        {
            base.Update();
            if(BodySprite.CurrentAnimation.Name == "respawn")
            {
                if(!BodySprite.IsPlaying) StateMachine.SetState("default");
                Entity.IsAlive = true;
                return;
            }
            ErVec2 diff = SwGame.ActiveCheckpoint.RectPx.Center - Entity.Position;
            double speed = Entity.BaseSpeed * SwGame.DeltaTime;
            double lenSq = diff.GetLengthSquared();
            if(lenSq < speed * speed)
            {
                Entity.Velocity = ErVec2.Zero;
                PlayBodyAnim("respawn");
            }
            else
            {
                ErVec2 dir = (SwGame.ActiveCheckpoint.RectPx.Center - Entity.Position).Normalized();
                Entity.Velocity = dir * Entity.BaseSpeed;
            }
        }
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
            if(CanAttack() && Controls.AttackJustPressed) StateMachine.SetState("attack");
            else if(Controls.IsCharging && Inventory.GetCount("sling_ammo") > 0) StateMachine.SetState("charging");
            else if(CanDodge() && Controls.DodgeJustPressed) StateMachine.SetState("dodging");
        }
    }
    public class Attack: SwPlayerState
    {
        public override string Name => "attack";
        protected override double StaminaRegenClockMul => 0;
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            SpoonSprite.Visible = true;
            SpoonSprite.Angle = (Controls.LastFacingIdx - 1) * ErMath.HALF_PI;
            SpoonSprite.Play();
            SetBodyHandedAnim(0, 0, Controls.LastFacingIdx);
            Entity.Velocity = ErVec2.Zero;
            SetHurtbox();
            Entity.Stamina -= Entity.SpoonAttackStaminaCost;
            Entity.StaminaRegenClock = Entity.StaminaRegenDelay;
            if(Entity.Stamina < 0) Entity.StaminaRegenClock += Entity.StaminaRegenDelayPenalty;
            SpoonSprite.HFlip = !SpoonSprite.HFlip;
        }
        public override void Update()
        {
            base.Update();
            if(!SpoonSprite.IsPlaying) StateMachine.SetState("default");
            SpoonHurtbox.Enabled = SpoonSprite.FrameIdx == 0;
        }
        public override void EndState(string nextState)
        {
            base.EndState(nextState);
            SpoonSprite.Visible = false;
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
        private bool CanFire()
        {
            if(!Controls.FireJustPressed) return false;
            if(!Controls.Aim.IsNonzero()) return false;
            return true;
        }
        private void Fire()
        {
            var pos = Entity.Position;
            var b = Entity.Props.Get("bullet").DeepCopy();
            if(!b.TryAs(out PriDict bullet)) throw new("fuck off");
            bullet.TrySet("x", pos.X);
            bullet.TrySet("y", pos.Y);
            bullet.TrySet("x_velocity", Controls.Aim.X * Entity.BulletSpeed);
            bullet.TrySet("y_velocity", Controls.Aim.Y * Entity.BulletSpeed);
            SwProjectile projectile = new();
            projectile.SetProps(bullet);
            SwGame.Game.AddEntity(projectile);
            Entity.AttackCooldownClock = 0.1;
            Entity.Ammo--;
            PriDict command = [];
            command.TrySet("verb", "hud_set");
            command.TrySet("key", "sling_ammo");
            command.TrySet("value", Entity.Ammo);
            SwApp.CommandStore.AddCommand(command);
        }
        public override void Update()
        {
            base.Update();
            int animIdx = Entity.Velocity.IsNonzero() ? 1 : 0;
            SetBodyHandedAnim(animIdx, 1, Controls.LastFacingIdx);
            Entity.Velocity = Controls.Move * Entity.BaseSpeed * Entity.ChargeSpeedMul;
            if (!Controls.IsCharging) StateMachine.SetState("default");
            else if (CanFire())
            {
                Fire();
                StateMachine.SetState("default");
            }
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
        protected override double StaminaRegenClockMul => 0;
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            BodySprite.Stop();
            SetBodyDodgeAnim(Controls.LastFacingIdx);
            Entity.Clock0 = 0;
            // set and lock in velocity
            Entity.Velocity = Controls.Move * Entity.BaseSpeed * Entity.DodgeSpeedMul;
            if(DustParticles.Particles is SwParticles2D particles)
            {
                particles.Emitting = true;
                particles.Speed = 30;
                particles.Amount = 80;
                particles.UseLocalCoords = false;
                particles.Lifetime = 5 * 0.125;
                particles.OneShot = true;
            }
            Entity.Stamina -= Entity.DodgeStaminaCost;
            Entity.StaminaRegenClock = Entity.StaminaRegenDelay;
            if(Entity.Stamina < 0) Entity.StaminaRegenClock += Entity.StaminaRegenDelayPenalty;
        }
        public override void Update()
        {
            base.Update();
            double elapsed = Entity.Clock0;
            Entity.Clock0 += SwGame.DeltaTime;
            if(!BodySprite.IsPlaying) StateMachine.SetState("default");
            // if(Entity.Clock0 > Entity.DodgeDuration) StateMachine.SetState("default");
            // Note: edge detection. fires when the clock is now past invuln delay
            else if(Entity.Clock0 >= Entity.DodgeInvulnDelay && elapsed < Entity.DodgeInvulnDelay) Entity.InvulnClock = Entity.DodgeInvulnWindow;
        }
        public override void EndState(string nextState)
        {
            base.EndState(nextState);
            Entity.DodgeCooldownClock = Entity.DodgeCooldown;
        }
    }
    public class ItemGet: SwPlayerState
    {
        public override string Name => "item_get";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            PlayBodyAnim("item_found");
            Entity.Velocity = ErVec2.Zero;
        }
        public override void Update()
        {
            base.Update();
            if(Controls.DodgeJustPressed) StateMachine.SetState("default");
        }
    }
    public static SwStateMachine GetStateMachine(SwPlayer parent, string name)
    {
        return new(parent, name, [
            new RespawnFadeIn(),
            new RespawnFadeOut(),
            new Respawn(),
            new Default(),
            new Attack(),
            new Charging(),
            new Charged(),
            new Dodging(),
            new Dead(),
            new ItemGet(),
        ]);
    }
}