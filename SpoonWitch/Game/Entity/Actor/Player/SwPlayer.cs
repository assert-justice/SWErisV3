using Eris;
using Eris.Renderer;
using ErisMath;
using ErisPhysics2D.Collider;
using Prion.Node;
using SpoonWitch.ByteStream;
using SpoonWitch.Command;
using SpoonWitch.Data;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Game.Map.Collision;
using SpoonWitch.Rendering;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Actor.Player;

public class SwPlayer: SwActor
{
    public override uint Mask => (uint)(IsAlive ? 3 : 0);
    public double ChargeTime => 1;
    public double ChargeSpeedMul => 0.5;
    // Note: dodge animations run at 12 fps, so 3/12 is 0.25 seconds
    public double DodgeInvulnDelay => 3.0 / 12;
    public double DodgeInvulnWindow => 4.0 / 12;
    public double DodgeDuration => 9.0 / 12;
    public double DodgeCooldown => 0.15;
    public double DodgeSpeedMul => 1.5;
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
    public double Mana = 100;
    public double MaxMana = 100;
    public double ManaRegen = 10;
    protected override int NumClocks => base.NumClocks + 3;
    public double Clock0{get => Clocks[base.NumClocks+0]; set {Clocks[base.NumClocks+0] = value;}}
    public double DodgeCooldownClock{get => Clocks[base.NumClocks+1]; set {Clocks[base.NumClocks+1] = value;}}
    public double AttackCooldownClock{get => Clocks[base.NumClocks+2]; set {Clocks[base.NumClocks+2] = value;}}
    private readonly SwStateMachine StateMachine;
    private readonly SwPlayerControls Controls;
    private SwInventoryComponent InventoryComp;
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
    // private static void SetHud(string key, double value)
    // {
    //     PriDict dict = [];
    //     dict.TrySet("verb", "hud_set");
    //     dict.TrySet("key", key);
    //     dict.TrySet("value", value);
    //     SwApp.CommandStore.AddCommand(dict);
    // }
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
        Props.TrySet("bullet/damage", slingDamage.ToPri());
        Props.TrySet("bullet/collision_mask", 3);
        InventoryComp.Entries.SetCount("sling_ammo", 0, 8);
        InventoryComp.Entries.SetCount("roots", 0, 1);
    }
    public override void Update()
    {
        base.Update();
        if(DodgeCooldownClock > 0) DodgeCooldownClock -= SwGame.DeltaTime;
        if(AttackCooldownClock > 0) AttackCooldownClock -= SwGame.DeltaTime;
        SwGame.SetPlayerPos(Position);
        Props.TrySet("spoon_damage/source_pos_x", Position.X);
        Props.TrySet("spoon_damage/source_pos_y", Position.Y);
        if(IsAlive && ErEngine.Input.GetKeyDown(SDL3.SDL.Scancode.Semicolon)) TestDamage(1000);
        if(IsAlive) SwGame.SetCameraTarget(Position);
    }
    protected override double Damage(SwDamage damage)
    {
        double value = base.Damage(damage);
        // if(value > 0)
        // {
        //     SetHud("health", Health);
        // }
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
        ErEngine.Log(command);
        if(!command.TryGet("ent_id", out int id)) return;
        if(!SwGame.Game.EntityLookup.TryGet<SwEntity>(id.ToString(), out var entity)) return;
        if(!command.TryGet("count", out int count)) return;
        if(!command.TryGet("pickup_type", out string pickup_type)) return;
        if(!InventoryComp.Entries.TryAdd(pickup_type, count, out int rem)) return;
        ErEngine.Log("lazy ", InventoryComp.Entries.GetCount("sling_ammo"));
        // SetHud(pickup_type, InventoryComp.Entries.GetCount(pickup_type));
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
