using Eris;
using ErisMath;
using Prion.Node;
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
    protected SwParticleComponent HealParticles = null!;
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
    protected static readonly string[] ReticleAnimations = [
        "still",
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
        if(!Entity.SpoonEnabled) return false;
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
    protected bool CanHeal()
    {
        if(Entity.Health >= Entity.MaxHealth) return false;
        return Entity.Roots > 0;
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
        HealParticles = Entity.GetComponent<SwParticleComponent>("heal_particles")!;
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
        if(Controls.PauseJustDown) SwApp.CommandQueue.AddCommandVerb("pause");
        ReticleSprite.Visible = Controls.IsReticleVisible;
        ReticleSprite.Offset = Controls.ReticlePosition;
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
        // public override void Update(double dt)
        // {
        //     base.Update(dt);
        //     if(BodySprite.IsPlaying) return;
        //     // Todo: drive this elsewhere
        //     if(BodySprite.CurrentAnimation.Name == "die") PlayBodyAnim("continue");
        //     else if(Entity.Game.Map.InSameRoom(Entity.Position, Entity.Game.Map.CurrentCheckpointPos)) StateMachine.SetState("respawn");
        //     else StateMachine.SetState("respawn_fade_out");
        // }
    }
    public class SwPlayerRevive: SwPlayerState
    {
        public override string Name => "revive";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            PlayBodyAnim("continue");
        }
        public override void Update(double dt)
        {
            base.Update(dt);
            if(BodySprite.IsPlaying) return;
            if(BodySprite.CurrentAnimation.Name == "continue") PlayBodyAnim("respawn");
            else StateMachine.SetState("default");
        }
    }
    public class Default: SwPlayerState
    {
        public override string Name => "default";
        public override void Update(double dt)
        {
            base.Update(dt);
            int animIdx = Entity.Velocity.IsNonzero() ? 1 : 0;
            SetBodyHandedAnim(animIdx, 2, Controls.LastFacingIdx);
            Entity.Velocity = Controls.Move * Entity.BaseSpeed;
            if(CanAttack() && Controls.AttackJustDown) StateMachine.SetState("attack");
            else if(Controls.IsCharging && Inventory.GetCount("sling_ammo") > 0) StateMachine.SetState("charging");
            else if(CanDodge() && Controls.DodgeJustDown) StateMachine.SetState("dodging");
            else if(CanHeal() && Controls.HealJustDown)
            {
                Entity.Health = Math.Clamp(Entity.Health + Entity.HealAmount, 0, Entity.MaxHealth);
                Entity.Roots -= 1;
                HealParticles.Particles.Emitting = true;
            }
            else if (Controls.UseJustDown)
            {
                PriDict command = [];
                command.TrySet("verb", "player_use");
                command.TrySet("player_idx", Entity.PlayerIdx);
                SwApp.CommandQueue.AddCommand(command);
            }
            else if(ErEngine.Input.GetKeyDown(SDL3.SDL.Scancode.Semicolon)) Entity.TestDamage(70);
            else if(ErEngine.Input.GetKeyDown(SDL3.SDL.Scancode.Leftbracket) && Entity.PlayerIdx == 0) Entity.TestDamage(70);
            else if(ErEngine.Input.GetKeyDown(SDL3.SDL.Scancode.Rightbracket) && Entity.PlayerIdx == 1) Entity.TestDamage(70);
            // else if(Entity.CurrentSpell is not null && !Entity.CurrentSpell.IsActive && CanCast() && Controls.CastJustDown)
            // {
            //     Entity.CurrentSpell.Begin();
            //     Entity.Mana -= Entity.CurrentSpell.ManaCost;
            // }
            // else if(Entity.CurrentSpell is not null && Entity.CurrentSpell.IsActive && Controls.CastJustDown){}
            else if(ErEngine.Input.GetKeyDown(SDL3.SDL.Scancode.T)) StateMachine.SetState("dancing");
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
            if(BodySprite.IsPlaying) return;
            if(Entity.TempData is not PriNull)
            {
                SwApp.CommandQueue.AddCommand(Entity.TempData);
            }
            StateMachine.SetState("default");
            // if(Controls.DodgeJustDown) StateMachine.SetState("default");
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
            if(Controls.DodgeJustDown) StateMachine.SetState("default");
        }
    }
    public static SwStateMachine<SwPlayer> GetStateMachine(SwPlayer parent, string name)
    {
        return new(parent, name, [
            new Default(),
            new SwPlayerRespawn(),
            new Attack(),
            new SwPlayerCharging(),
            new SwPlayerCharged(),
            new SwPlayerDodging(),
            new Dead(),
            new SwPlayerRevive(),
            new ItemGet(),
            new Dancing(),
        ]);
    }
}