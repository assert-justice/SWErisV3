using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Data;
using SpoonWitch.Game.Effect;
using SpoonWitch.Game.Effect.Spell;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Game.Inventory;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.Actor.Player;

public class SwPlayer: SwActor
{
    public override uint Mask => (uint)(IsAlive ? 3 : 0);
    public override int RenderLayer => 3;
    public int PlayerIdx;
    // Health
    // Note: Health and MaxHealth defined in SwActor
    // Stamina
    public double Stamina = 100;
    public double MaxStamina = 100;
    public double StaminaRegen = 30;
    public double StaminaRegenDelay = 0.1;
    public double StaminaRegenDelayPenalty = 0.3;
    public double StaminaRegenClock
    {
        get => Clocks[0];
        set => Clocks[0] = value;
    }
    // Mana
    public double Mana = 100;
    public double MaxMana = 100;
    public double ManaRegen = 10;
    // Speed
    // Note: BaseSpeed is defined in SwActor
    public double SlowedSpeedMul = 0.5;
    // Dodge
    public double DodgeSpeedMul = 1.5;
    public double DodgeDuration = 9.0 / 8;
    public double DodgeInvulnDelay = 3.0 / 8;
    public double DodgeInvulnDuration = 4.0 / 8;
    public double DodgeCooldown = 0.15;
    public double DodgeStaminaCost = 20;
    public double DodgeCooldownClock
    {
        get => Clocks[1];
        set => Clocks[1] = value;
    }
    // Spoon
    // Note: SpoonDamage stays in props
    public double SpoonSwingDuration = 0.625;
    public double SpoonHurtDelay = 0.125;
    public double SpoonHurtDuration = 0.125;
    public double SpoonRecoveryTime = 0.125;
    public double SpoonStaminaCost = 30;
    public double SpoonCooldownClock
    {
        get => Clocks[2];
        set => Clocks[2] = value;
    }
    // Sling
    // Note: Likewise, SlingDamage stays in props
    public double SlingBulletSpeed = 600;
    public double SlingChargeTime = 0.75;
    public double SlingChargeClock
    {
        get => Clocks[3];
        set => Clocks[3] = value;
    }
    // Inventory
    public readonly SwInventory Inventory = new();
    public int Ammo
    {
        get => Inventory.GetCount("sling_ammo");
        set => Inventory.SetCount("sling_ammo", value);
    }
    public int MaxAmmo
    {
        get => Inventory.GetMax("sling_ammo");
        set => Inventory.SetCount("sling_ammo", Ammo, value);
    }
    public int Roots
    {
        get => Inventory.GetCount("roots");
        set => Inventory.SetCount("roots", value);
    }
    public int MaxRoots
    {
        get => Inventory.GetMax("roots");
        set => Inventory.SetCount("roots", Ammo, value);
    }
    public SwSpell? CurrentSpell;
    public SwStateMachine<SwPlayer>? StateMachine{get; private set;}
    public ErTexture? PickupTexture;
    private bool GotMad = false;
    public SwPlayer()
    {
        AddHandler("ent_offer_item", EntOfferItem);
        AddGlobalHandler("player_add_item", PlayerAddItem);
    }
    private void OnEnterSpoonHurtbox(SwEntity entity)
    {
        if(!Props.TryGet("spoon/spoon_damage", out PriNode spoonDamage)) return;
        entity.AddCommand(spoonDamage);
    }
    public override void SetProps(PriNode props)
    {
        base.SetProps(props);
        // Health
        // Note: Health and MaxHealth are handled in SwActor
        // Stamina
        if(Props.TryGet("stamina/max_stamina", out double d)) MaxStamina = d;
        if(Props.TryGet("stamina/stamina", out d)) Stamina = d;
        if(Props.TryGet("stamina/stamina_regen", out d)) StaminaRegen = d;
        if(Props.TryGet("stamina/stamina_regen_delay", out d)) StaminaRegenDelay = d;
        if(Props.TryGet("stamina/stamina_regen_delay_penalty", out d)) StaminaRegenDelayPenalty = d;
        // Mana
        if(Props.TryGet("mana/max_mana", out d)) MaxMana = d;
        if(Props.TryGet("mana/mana", out d)) Mana = d;
        if(Props.TryGet("mana/mana_regen", out d)) ManaRegen = d;
        // Speed
        if(Props.TryGet("speed/base_speed", out d)) BaseSpeed = d;
        if(Props.TryGet("speed/slowed_speed_mul", out d)) SlowedSpeedMul = d;
        // Dodge
        if(Props.TryGet("dodge/dodge_speed_mul", out d)) DodgeSpeedMul = d;
        if(Props.TryGet("dodge/dodge_duration", out d)) DodgeDuration = d;
        if(Props.TryGet("dodge/dodge_invuln_delay", out d)) DodgeInvulnDelay = d;
        if(Props.TryGet("dodge/dodge_invuln_duration", out d)) DodgeInvulnDuration = d;
        if(Props.TryGet("dodge/dodge_cooldown", out d)) DodgeCooldown = d;
        if(Props.TryGet("dodge/dodge_stamina_cost", out d)) DodgeStaminaCost = d;
        // Spoon
        // Note: SpoonDamage stays in props
        if(Props.TryGet("spoon/spoon_swing_duration", out d)) SpoonSwingDuration = d;
        if(Props.TryGet("spoon/spoon_hurt_delay", out d)) SpoonHurtDelay = d;
        if(Props.TryGet("spoon/hurt_duration", out d)) SpoonHurtDuration = d;
        if(Props.TryGet("spoon/spoon_recovery_time", out d)) SpoonRecoveryTime = d;
        if(Props.TryGet("spoon/spoon_stamina_cost", out d)) SpoonStaminaCost = d;
        // Sling
        // Note: Likewise, SlingDamage stays in props
        if(Props.TryGet("sling/sling_bullet_speed", out d)) SlingBulletSpeed = d;
        if(Props.TryGet("sling/sling_charge_time", out d)) SlingChargeTime = d;
        // Inventory
        Inventory.SetData(Props.Get("inventory"));
        // var spell = new SwCometShield(this);
        // CurrentSpell = spell;
        Clocks.Reset();
    }
    public override void Init()
    {
        base.Init();
        // Register components
        LoadSprites("anim_data/sprites");
        var Controls = new SwPlayerControls(this);
        RegisterComponent(Controls);
        if(!SwParticles2D.TryFromData(out var particles, Props.Get("dust_particles"))) ErEngine.LogWarning("unable to read player dust particles");
        else RegisterComponent(new SwParticleComponent(this, "dust_particles", particles));
        var SpoonHurtbox = new SwAreaComponent(this, "spoon_hurtbox", 4, new(32, 32), onBodyEnter: OnEnterSpoonHurtbox);
        RegisterComponent(SpoonHurtbox);
        StateMachine = SwPlayerState.GetStateMachine(this, "state_machine");
        RegisterComponent(StateMachine);
    }
    protected override void Update(double dt)
    {
        base.Update(dt);
        Props.TrySet("spoon_damage/source_pos_x", Position.X);
        Props.TrySet("spoon_damage/source_pos_y", Position.Y);
        CurrentSpell?.Update();
        if(IsAlive && ErEngine.Input.GetKeyDown(SDL3.SDL.Scancode.Semicolon)) TestDamage(1000);
        if(IsAlive) Game.AddFocusPoint(Position);
        if(!GotMad && ErEngine.Input.GetKeyDown(SDL3.SDL.Scancode.M))
        {
            SwApp.CommandQueue.AddCommandVerb("get_mad");
            GotMad = true;
        }
    }
    protected override void UpdateLate(double dt)
    {
        base.UpdateLate(dt);
    }
    protected override void Draw()
    {
        base.Draw();
        if(PickupTexture is not null)
        {
            var rect = ErRect2.Centered(Position + ErVec2.Up * 24, PickupTexture.Size);
            PickupTexture.Draw(rect.Position);
        }
    }
    protected override double Damage(SwDamage damage)
    {
        double value = base.Damage(damage);
        return value;
    }
    protected override void Die()
    {
        base.Die();
        StateMachine?.SetState("dead");
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
        if(!Game.EntityLookup.TryGet<SwEntity>(id.ToString(), out var entity)) return;
        if(!command.TryGet("count", out int count)) return;
        if(!command.TryGet("pickup_type", out string pickup_type)) return;
        if(!Inventory.TryAdd(pickup_type, count, out int rem)) return;
        PriDict com = [];
        com.TrySet("verb", "pickup_set_rem");
        com.TrySet("rem", rem);
        entity.AddCommand(com);
    }
    private void PlayerAddItem(PriNode command)
    {
        StateMachine?.SetState("item_get");
        if(!command.TryGet("pickup_type", out string pickup_type)) return;
        if(SwData.Prototypes.TryGet($"pickups/{pickup_type}/texture_filepath", out string texture_filepath))
        {
            if(!ErTexture.TryFromPath(texture_filepath, out PickupTexture)) ErEngine.Log("bad pickup texture path");
        }
    }
}
