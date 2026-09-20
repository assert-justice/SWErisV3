using Eris;
using Eris.Renderer;
using ErisMath;
using ErisPhysics2D.Collider;
using Prion.Node;
using SpoonWitch.ByteStream;
using SpoonWitch.Command;
using SpoonWitch.Data;
using SpoonWitch.Game.Effect.Spell;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Game.Map.Collision;
using SpoonWitch.Rendering;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Actor.Player;

public class SwPlayer: SwActor
{
    public override uint Mask => (uint)(IsAlive ? 3 : 0);
    public override int RenderLayer => 3;
    public double ChargeTime => 1;
    public double ChargeSpeedMul => 0.5;
    // Note: dodge animations run at 12 fps, so 3/12 is 0.25 seconds
    public double DodgeInvulnDelay => 3.0 / 12;
    public double DodgeInvulnWindow => 4.0 / 12;
    public double DodgeDuration => 9.0 / 12;
    public double DodgeCooldown => 0.15;
    public double DodgeSpeedMul => 1.5;
    public double DodgeStaminaCost = 20;
    public double BulletSpeed => 100;
    public int Ammo
    {
        get => InventoryComp.Entries.GetCount("sling_ammo");
        set => InventoryComp.Entries.SetCount("sling_ammo", value);
    }
    public int MaxAmmo
    {
        get => InventoryComp.Entries.GetMax("sling_ammo");
        set => InventoryComp.Entries.SetCount("sling_ammo", Ammo, value);
    }
    public int Roots => InventoryComp.Entries.GetCount("roots");
    public int MaxRoots => InventoryComp.Entries.GetMax("roots");
    public double Stamina = 100;
    public double MaxStamina = 100;
    public double StaminaRegen = 30;
    public double StaminaRegenDelay = 0.1;
    public double StaminaRegenDelayPenalty = 0.3;
    public double StaminaRegenClock = 0;
    public double Mana = 100;
    public double MaxMana = 100;
    public double ManaRegen = 10;
    protected override int NumClocks => base.NumClocks + 3;
    public double Clock0{get => Clocks[base.NumClocks+0]; set {Clocks[base.NumClocks+0] = value;}}
    public double DodgeCooldownClock{get => Clocks[base.NumClocks+1]; set {Clocks[base.NumClocks+1] = value;}}
    public double AttackCooldownClock{get => Clocks[base.NumClocks+2]; set {Clocks[base.NumClocks+2] = value;}}
    public double SpoonAttackStaminaCost = 30;
    public SwSpell? CurrentSpell;
    private readonly SwStateMachine StateMachine;
    private readonly SwPlayerControls Controls;
    private readonly SwInventoryComponent InventoryComp;
    public SwPlayer()
    {
        Controls = new SwPlayerControls(this);
        RegisterComponent(Controls);
        InventoryComp = new SwInventoryComponent(this, "inventory");
        RegisterComponent(InventoryComp);
        if(!SwApp.TryLoadPrion("game_data/particles/particles.json", out var animData)) throw new("bad");
        SwAnimation.TryFromPriAse(out var animation, "dust_1", "game_data/particles", animData);
        SwParticleComponent particles = new(this, "dust_1", animation)
        {
            Offset = new(0, 9)
        };
        RegisterComponent(particles);
        string path = "game_data/entities/actors/player/player_anim_data.json";
        if(!TryLoadSprites(path)) ErEngine.LogWarning("failed to load player sprites");
        SwAreaComponent spoonHurtbox = new(this, "spoon_hurtbox", 4, new(32, 32), onBodyEnter: OnEnterSpoonHurtbox);
        RegisterComponent(spoonHurtbox);
        StateMachine = SwPlayerState.GetStateMachine(this, "state_machine");
        RegisterComponent(StateMachine);
        AddHandler("ent_offer_item", EntOfferItem);
        AddGlobalHandler("player_add_item", PlayerAddItem);
        Size = new(28, 28);
    }
    private void OnEnterSpoonHurtbox(SwEntity entity)
    {
        if(!Props.TryGet("spoon_damage", out PriNode spoonDamage)) return;
        ErEngine.Log(spoonDamage);
        entity.AddCommand(spoonDamage);
    }
    public override void Ready()
    {
        base.Ready();
        IsAlive = false;
        SwDamage spoonDamage = new([(SwDamageType.Untyped, 10)]);
        var d = spoonDamage.ToPri();
        Props.TrySet("spoon_damage", d);
        InventoryComp.Entries.SetCount("sling_ammo", 0, 10);
        SwDamage slingDamage = new([(SwDamageType.Untyped,10)]);
        PriDict impactParticles = [];
        impactParticles.TrySet("name", "rock_chunks");
        impactParticles.TrySet("filepath_ase", "game_data/particles/particles.json");
        impactParticles.TrySet("explosiveness", 0.75);
        impactParticles.TrySet("one_shot", true);
        impactParticles.TrySet("lifetime", 0.1);
        impactParticles.TrySet("amount", 20);
        impactParticles.TrySet("speed", 100);
        impactParticles.TrySet("randomize_frames", true);
        PriDict flyingParticles = [];
        flyingParticles.TrySet("name", "sparkles");
        flyingParticles.TrySet("filepath_texture", "game_data/particles/blue_sparkle.png");
        flyingParticles.TrySet("lifetime", 0.3);
        flyingParticles.TrySet("use_local_coordinates", false);
        flyingParticles.TrySet("amount", 20);
        flyingParticles.TrySet("speed", 100);
        flyingParticles.TrySet("emitting", true);
        Props.TrySet("bullet/damage", slingDamage.ToPri());
        Props.TrySet("bullet/collision_mask", 3);
        Props.TrySet("bullet/texture_filepath", "game_data/entities/actors/player/images/bella_sling_ammo_shot.png");
        Props.TrySet("bullet/impact_particles", impactParticles);
        Props.TrySet("comet/damage", slingDamage.ToPri());
        Props.TrySet("comet/collision_mask", 3);
        Props.TrySet("comet/texture_filepath", "game_data/entities/actors/player/images/bella_sling_ammo_shot.png");
        Props.TrySet("comet/impact_particles", impactParticles);
        Props.TrySet("comet/flying_particles", flyingParticles);
        var spell = new SwCometShield(this)
        {
            ProjectileData = Props.Get("comet"),
        };
        CurrentSpell = spell;
        InventoryComp.Entries.SetCount("sling_ammo", 0, 8);
        InventoryComp.Entries.SetCount("roots", 0, 1);

    }
    public override void Update()
    {
        base.Update();
        SwGame.SetPlayerPos(Position);
        Props.TrySet("spoon_damage/source_pos_x", Position.X);
        Props.TrySet("spoon_damage/source_pos_y", Position.Y);
        CurrentSpell?.Update();
        if(IsAlive && ErEngine.Input.GetKeyDown(SDL3.SDL.Scancode.Semicolon)) TestDamage(1000);
        if(IsAlive) SwGame.SetCameraTarget(Position);
    }
    protected override double Damage(SwDamage damage)
    {
        double value = base.Damage(damage);
        return value;
    }
    protected override void Die()
    {
        base.Die();
        StateMachine.SetState("dead");
    }
    public override void GameCleanup()
    {
        base.GameCleanup();
    }
    public void TestDamage(double value)
    {
        SwDamage damage = new([(SwDamageType.Untyped,value)]);
        Damage(damage);
    }
    private void EntOfferItem(PriNode command)
    {
        if(!command.TryGet("ent_id", out int id)) return;
        if(!SwGame.Game.EntityLookup.TryGet<SwEntity>(id.ToString(), out var entity)) return;
        if(!command.TryGet("count", out int count)) return;
        if(!command.TryGet("pickup_type", out string pickup_type)) return;
        if(!InventoryComp.Entries.TryAdd(pickup_type, count, out int rem)) return;
        PriDict com = [];
        com.TrySet("verb", "pickup_set_rem");
        com.TrySet("rem", rem);
        entity.AddCommand(com);
    }
    private void PlayerAddItem(PriNode command)
    {
        StateMachine.SetState("item_get");
    }
}
