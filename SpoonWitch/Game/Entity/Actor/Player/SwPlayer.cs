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
    public double StaminaRegenClock = 0;
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
    public double DodgeCooldownClock = 0;
    // Spoon
    // Note: SpoonDamage stays in props
    public double SpoonSwingDuration = 0.625;
    public double SpoonHurtDelay = 0.125;
    public double SpoonHurtDuration = 0.125;
    public double SpoonRecoveryTime = 0.125;
    public double SpoonStaminaCost = 30;
    public double SpoonCooldownClock = 0;
    // Sling
    // Note: Likewise, SlingDamage stays in props
    public double SlingBulletSpeed = 600;
    public double SlingChargeTime = 0.75;
    public double SlingChargeClock = 0;
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
    public SwStateMachine? StateMachine{get; private set;}
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
    protected override void SetProps(PriNode props)
    {
        base.SetProps(props);
        // Set Properties
        // Health
        // Note: Health and MaxHealth are handled in SwActor
        // Stamina
        Stamina = Props.TryGet("stamina/stamina", out double d) ? d : 100;
        MaxStamina = Props.TryGet("stamina/max_stamina", out d) ? d : 100;
        StaminaRegen = Props.TryGet("stamina/stamina_regen", out d) ? d : 30;
        StaminaRegenDelay = Props.TryGet("stamina/stamina_regen_delay", out d) ? d : 0.1;
        StaminaRegenDelayPenalty = Props.TryGet("stamina/stamina_regen_delay_penalty", out d) ? d : 0.3;
        // Mana
        Mana = Props.TryGet("mana/mana", out d) ? d : 100;
        MaxMana = Props.TryGet("mana/max_mana", out d) ? d : 100;
        ManaRegen = Props.TryGet("mana/mana_regen", out d) ? d : 10;
        // Speed
        BaseSpeed = Props.TryGet("speed/base_speed", out d) ? d : 100;
        SlowedSpeedMul = Props.TryGet("speed/slowed_speed_mul", out d) ? d : 0.5;
        // Dodge
        DodgeSpeedMul = Props.TryGet("dodge/dodge_speed_mul", out d) ? d : 1.5;
        DodgeDuration = Props.TryGet("dodge/dodge_duration", out d) ? d : 9.0 / 8;
        DodgeInvulnDelay = Props.TryGet("dodge/dodge_invuln_delay", out d) ? d : 3.0 / 8;
        DodgeInvulnDuration = Props.TryGet("dodge/dodge_invuln_duration", out d) ? d : 4.0 / 8;
        DodgeCooldown = Props.TryGet("dodge/dodge_cooldown", out d) ? d : 0.15;
        DodgeStaminaCost = Props.TryGet("dodge/dodge_stamina_cost", out d) ? d : 20;
        // Spoon
        // Note: SpoonDamage stays in props
        SpoonSwingDuration = Props.TryGet("spoon/spoon_swing_duration", out d) ? d : 0.625;
        SpoonHurtDelay = Props.TryGet("spoon/spoon_hurt_delay", out d) ? d : 0.125;
        SpoonHurtDuration = Props.TryGet("spoon/spoon_hurt_duration", out d) ? d : 0.125;
        SpoonRecoveryTime = Props.TryGet("spoon/spoon_recovery_time", out d) ? d : 0.125;
        SpoonStaminaCost = Props.TryGet("spoon/spoon_stamina_cost", out d) ? d : 30;
        // Sling
        // Note: Likewise, SlingDamage stays in props
        SlingBulletSpeed = Props.TryGet("sling/sling_bullet_speed", out d) ? d : 600;
        SlingChargeTime = Props.TryGet("sling/sling_charge_time", out d) ? d : 0.75;
        // Inventory
        Inventory.SetData(Props.Get("inventory"));
        var spell = new SwCometShield(this);
        CurrentSpell = spell;
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
    public override void Ready()
    {
        base.Ready();
        IsAlive = false;
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
        if(!GotMad && ErEngine.Input.GetKeyDown(SDL3.SDL.Scancode.M))
        {
            SwApp.CommandStore.AddCommandVerb("get_mad");
            GotMad = true;
        }
    }
    protected override void DrawImpl(SwEntity nextState)
    {
        base.DrawImpl(nextState);
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
        if(!SwGame.Game.EntityLookup.TryGet<SwEntity>(id.ToString(), out var entity)) return;
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
        // if(!command.TryGet("text", out string text)) text = string.Empty;
        if(SwData.Prototypes.TryGet($"pickups/{pickup_type}/texture_filepath", out string texture_filepath))
        {
            if(!ErTexture.TryFromPath(texture_filepath, out PickupTexture)) ErEngine.Log("bad pickup texture path");
        }
    }
}
