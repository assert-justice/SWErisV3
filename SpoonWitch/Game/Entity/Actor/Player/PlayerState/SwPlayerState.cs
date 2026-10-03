using Eris;
using ErisMath;
using SpoonWitch.Data;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Game.Entity.Projectile;
using SpoonWitch.Game.Inventory;
using SpoonWitch.Rendering;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Actor.Player.PlayerState;

public abstract class SwPlayerState : SwState<SwPlayer>
{
    protected SwSprite BodySprite = null!;
    protected SwSprite HatSprite = null!;
    protected SwSprite SpoonSprite = null!;
    protected SwSprite SlingSprite = null!;
    protected SwSprite ReticleSprite = null!;
    protected SwPlayerInput Controls = null!;
    protected SwAreaComponent SpoonHurtbox = null!;
    protected SwParticleComponent DustParticles = null!;
    protected SwInventory Inventory => Entity.Inventory;
    protected virtual double StaminaRegenClockMul => 1;
    protected virtual double ManaRegenMul => 1;
    // name, hands, facing
    private static readonly string[][][] BodyAnimations = [
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
    private static readonly string[] DodgeAnimations = [
        "def_dodge_dr",
        "def_dodge_d",
        "def_dodge_dl",
        "def_dodge_u",
    ];
    private static readonly string[] ReticleAnimations = [
        "charge_0",
        "charge_1",
        "charge_2",
        "charge_3",
    ];
    public SwPlayerState(){}
    protected bool CanDodge()
    {
        if(Entity.DodgeCooldownClock.IsRunning) return false;
        if(!Controls.Move.IsNonzero()) return false;
        if(Entity.Stamina <= 0) return false;
        return true;
    }
    protected bool CanAttack()
    {
        if(Entity.SpoonCooldownClock.IsRunning) return false;
        if(Entity.Stamina <= 0) return false;
        return true;
    }
    protected bool CanCast()
    {
        if(Entity.CurrentSpell is null) return false;
        if(Entity.Mana < Entity.CurrentSpell.ManaCost) return false;
        if(Entity.CurrentSpell.IsActive) return false;
        return true;
    }
    public override void Ready()
    {
        base.Ready();
        BodySprite = Entity.GetComponent<SwSpriteComponent>("body")?.Sprite!;
        HatSprite = Entity.GetComponent<SwSpriteComponent>("hat")?.Sprite!;
        SpoonSprite = Entity.GetComponent<SwSpriteComponent>("spoon")?.Sprite!;
        SlingSprite = Entity.GetComponent<SwSpriteComponent>("sling")?.Sprite!;
        ReticleSprite = Entity.GetComponent<SwSpriteComponent>("reticle")?.Sprite!;
        DustParticles = Entity.GetComponent<SwParticleComponent>("dust_particles")!;
        Controls = Entity.GetComponent<SwPlayerControls>("controls")?.InputDevice!;
        SpoonHurtbox = Entity.GetComponent<SwAreaComponent>("spoon_hurtbox")!;
    }
    protected void SetBodyHandedAnim(int animIdx, int hands, int facing)
    {
        string animName = BodyAnimations[animIdx][hands][facing];
        BodySprite.Play(animName);
        HatSprite.Play(animName);
    }
    protected void SetBodyDodgeAnim(int facing)
    {
        string animName = DodgeAnimations[facing];
        BodySprite.Play(animName);
        HatSprite.Play(animName);
    }
    protected void PlayBodyAnim(string animName)
    {
        BodySprite.Play(animName);
        HatSprite.Play(animName);
    }
    // public override void BeginState(string lastState)
    // {
    //     base.BeginState(lastState);
    //     ErEngine.Log(Name);
    // }
    public override void Update(double dt)
    {
        base.Update(dt);
        if(Controls.PauseJustPressed) SwApp.CommandQueue.AddCommandVerb("pause");
        // ReticleSprite.Visible = Controls.ReticleVisible;
        // ReticleSprite.Offset = Controls.ReticlePosition;
        if(Entity.Stamina < Entity.MaxStamina && !Entity.StaminaRegenClock.IsRunning)
        {
            Entity.Stamina += Entity.StaminaRegen * dt;
            if(Entity.Stamina > Entity.MaxStamina) Entity.Stamina = Entity.MaxStamina;
        }
        if(Entity.Mana < Entity.MaxMana)
        {
            Entity.Mana += Entity.ManaRegen * dt * ManaRegenMul;
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
        public override void Update(double dt)
        {
            base.Update(dt);
            if(BodySprite.IsPlaying) return;
            // Todo: drive this elsewhere
            if(BodySprite.CurrentAnimation.Name == "die") PlayBodyAnim("continue");
            // else if(Entity.Game.Map.InSameRoom(Entity.Position, SwGame.ActiveCheckpoint.RectPx.Center)) StateMachine.SetState("respawn");
            else StateMachine.SetState("respawn_fade_out");
        }
    }
    // public class RespawnFadeOut: SwPlayerState
    // {
    //     public override string Name => "respawn_fade_out";
    //     public override void BeginState(string lastState)
    //     {
    //         base.BeginState(lastState);
    //         PlayBodyAnim("fly");
    //         SwGame.Game.FadeOut();
    //     }
    //     public override void Update()
    //     {
    //         base.Update();
    //         bool isVisible = SwGame.Camera.IsPointVisible(Entity.Position);
    //         if (isVisible)
    //         {
    //             Entity.MoveToward(SwGame.ActiveCheckpoint.RectPx.Center, Entity.BaseSpeed);
    //         }
    //         else Entity.Velocity = ErVec2.Zero;
    //         if(SwGame.Game.FadeState == 1 && !isVisible) StateMachine.SetState("respawn_fade_in");
    //     }
    // }
    // public class RespawnQuick: SwPlayerState
    // {
    //     public override string Name => "quick_spawn";
    //     public override void BeginState(string lastState)
    //     {
    //         base.BeginState(lastState);
    //         SwGame.SetCameraTarget(SwGame.ActiveCheckpoint.RectPx.Center, true);
    //         Entity.Position = SwGame.ActiveCheckpoint.RectPx.Center;
    //     }
    //     public override void Update()
    //     {
    //         base.Update();
    //         Entity.IsAlive = true;
    //         StateMachine.SetState("default");
    //     }
    // }
    // public class RespawnFadeIn: SwPlayerState
    // {
    //     public override string Name => "respawn_fade_in";
    //     public override void BeginState(string lastState)
    //     {
    //         base.BeginState(lastState);
    //         PlayBodyAnim("fly");
    //         SwGame.Game.FadeIn();
    //         SwGame.SetCameraTarget(SwGame.ActiveCheckpoint.RectPx.Center, true);
    //         ErVec2 diff = Entity.Position - SwGame.ActiveCheckpoint.RectPx.Center;
    //         double distance = diff.GetLength();
    //         if(500 < distance) distance = 500;
    //         ErVec2 offset = diff.Normalized() * distance;
    //         ErVec2 pos = SwGame.ActiveCheckpoint.RectPx.Center + offset;
    //         Entity.Position = pos;
    //     }
    //     public override void Update()
    //     {
    //         base.Update();
    //         if(BodySprite.CurrentAnimation.Name == "respawn")
    //         {
    //             if (!BodySprite.IsPlaying)
    //             {
    //                 StateMachine.SetState("default");
    //                 Entity.IsAlive = true;
    //             }
    //             return;
    //         }
    //         double distance = Entity.MoveToward(SwGame.ActiveCheckpoint.RectPx.Center, Entity.BaseSpeed);
    //         if(distance == 0)
    //         {
    //             PlayBodyAnim("respawn");
    //         }
    //     }
    // }
    // public class Respawn: SwPlayerState
    // {
    //     public override string Name => "respawn";
    //     public override void BeginState(string lastState)
    //     {
    //         base.BeginState(lastState);
    //         PlayBodyAnim("fly");
    //     }
    //     public override void Update()
    //     {
    //         base.Update();
    //         if(BodySprite.CurrentAnimation.Name == "respawn")
    //         {
    //             if(!BodySprite.IsPlaying) StateMachine.SetState("default");
    //             Entity.IsAlive = true;
    //             return;
    //         }
    //         ErVec2 diff = SwGame.ActiveCheckpoint.RectPx.Center - Entity.Position;
    //         double speed = Entity.BaseSpeed * SwGame.DeltaTime;
    //         double lenSq = diff.GetLengthSquared();
    //         if(lenSq < speed * speed)
    //         {
    //             Entity.Velocity = ErVec2.Zero;
    //             PlayBodyAnim("respawn");
    //         }
    //         else
    //         {
    //             ErVec2 dir = (SwGame.ActiveCheckpoint.RectPx.Center - Entity.Position).Normalized();
    //             Entity.Velocity = dir * Entity.BaseSpeed;
    //         }
    //     }
    // }
    public class Default: SwPlayerState
    {
        public override string Name => "default";
        public override void Update(double dt)
        {
            base.Update(dt);
            int animIdx = Entity.Velocity.IsNonzero() ? 1 : 0;
            SetBodyHandedAnim(animIdx, 2, Controls.LastFacingIdx);
            Entity.Velocity = Controls.Move * Entity.BaseSpeed;
            if(CanAttack() && Controls.AttackJustPressed) StateMachine.SetState("attack");
            else if(Controls.IsCharging && Inventory.GetCount("sling_ammo") > 0) StateMachine.SetState("charging");
            else if(CanDodge() && Controls.DodgeJustPressed) StateMachine.SetState("dodging");
            else if(Entity.CurrentSpell is not null && !Entity.CurrentSpell.IsActive && CanCast() && Controls.CastJustPressed)
            {
                Entity.CurrentSpell.Begin();
                Entity.Mana -= Entity.CurrentSpell.ManaCost;
            }
            else if(Entity.CurrentSpell is not null && Entity.CurrentSpell.IsActive && Controls.CastJustPressed){}
            else if(ErEngine.Input.HandleKeyDown(SDL3.SDL.Scancode.T)) StateMachine.SetState("dancing");
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
            Entity.UseStamina(Entity.SpoonStaminaCost);
            SpoonSprite.HFlip = !SpoonSprite.HFlip;
        }
        public override void Update(double dt)
        {
            base.Update(dt);
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
    // public class Charging: SwPlayerState
    // {
    //     public override string Name => "charging";
    //     private const int NumThresholds = 3;
    //     private int Threshold = 0;
    //     public override void BeginState(string lastState)
    //     {
    //         base.BeginState(lastState);
    //         SlingSprite.Visible = true;
    //         SlingSprite.Play("charging");
    //         ReticleSprite.Play(ReticleAnimations[0]);
    //         Entity.SlingChargeClock.Start(Entity.SlingChargeTime / NumThresholds);
    //         Threshold = 0;
    //     }
    //     public override void Update(double dt)
    //     {
    //         base.Update(dt);
    //         int animIdx = Entity.Velocity.IsNonzero() ? 1 : 0;

    //         SetBodyHandedAnim(animIdx, 1, Controls.LastFacingIdx);
    //         Entity.Velocity = Controls.Move * Entity.BaseSpeed * Entity.SlowedSpeedMul;
    //         if (!Controls.IsCharging)
    //         {
    //             SlingSprite.Visible = false;
    //             SlingSprite.Stop();
    //             ReticleSprite.Play("still");
    //             StateMachine.SetState("default");
    //             return;
    //         }
    //         if (!Entity.SlingChargeClock.IsRunning)
    //         {
    //             if(Threshold == NumThresholds) StateMachine.SetState("charged");
    //             else
    //             {
    //                 Threshold++;
    //                 // Todo: set reticle sprite
    //                 Entity.SlingChargeClock.Restart();
    //             }
    //         }
    //     }
    // }
    // public class Charged: SwPlayerState
    // {
    //     public override string Name => "charged";
    //     public override void BeginState(string lastState)
    //     {
    //         base.BeginState(lastState);
    //         SlingSprite.Play("charged");
    //     }
    //     private bool CanFire()
    //     {
    //         if(!Controls.FireJustPressed) return false;
    //         if(!Controls.Aim.IsNonzero()) return false;
    //         return true;
    //     }
    //     private void Fire()
    //     {
    //         Entity.Ammo--;
    //         var sling = Entity.Props.Get("sling");
    //         if(!Entity.Props.TryGet("sling/projectile", out string slingProto)) return;
    //         var props = SwData.Prototypes.Get($"projectiles/{slingProto}");
    //         SwPrion.TrySetVec2(props, "velocity", Controls.Aim * Entity.SlingBulletSpeed);
    //         SwPrion.TrySetVec2(props, Entity.Position);
    //         props.TrySet("damage", sling.Get("sling_damage"));
    //         Entity.Game.AddEntity<SwProjectile>(props);
    //     }
    //     public override void Update(double dt)
    //     {
    //         base.Update(dt);
    //         int animIdx = Entity.Velocity.IsNonzero() ? 1 : 0;
    //         SetBodyHandedAnim(animIdx, 1, Controls.LastFacingIdx);
    //         Entity.Velocity = Controls.Move * Entity.BaseSpeed * Entity.SlowedSpeedMul;
    //         if (!Controls.IsCharging) StateMachine.SetState("default");
    //         else if (CanFire())
    //         {
    //             Fire();
    //             StateMachine.SetState("default");
    //         }
    //     }
    //     public override void EndState(string nextState)
    //     {
    //         base.EndState(nextState);
    //         SlingSprite.Visible = false;
    //         SlingSprite.Stop();
    //         ReticleSprite.Play("still");
    //     }
    // }
    public class ItemGet: SwPlayerState
    {
        public override string Name => "item_get";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            PlayBodyAnim("item_found");
            Entity.Velocity = ErVec2.Zero;
        }
        public override void Update(double dt)
        {
            base.Update(dt);
            if(Controls.DodgeJustPressed) StateMachine.SetState("default");
        }
        public override void EndState(string nextState)
        {
            base.EndState(nextState);
            Entity.PickupTexture = null;
        }
    }
    public class Dancing: SwPlayerState
    {
        public override string Name => "dancing";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            PlayBodyAnim("dance");
            Entity.Velocity = ErVec2.Zero;
        }
        public override void Update(double dt)
        {
            base.Update(dt);
            if(Controls.DodgeJustPressed) StateMachine.SetState("default");
        }
    }
    public static SwStateMachine<SwPlayer> GetStateMachine(SwPlayer parent, string name)
    {
        return new(parent, name, [
            // new RespawnQuick(),
            // new RespawnFadeIn(),
            // new RespawnFadeOut(),
            // new Respawn(),
            new Default(),
            new Attack(),
            new SwPlayerCharging(),
            new SwPlayerCharged(),
            new SwPlayerDodging(),
            new Dead(),
            new ItemGet(),
            new Dancing(),
        ]);
    }
}