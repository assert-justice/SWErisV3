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

public class SwPlayer: SwActor, ISwEntity<SwPlayer>
{
    private static SwPlayer? _Primary;
    private static SwPlayer? _Secondary;
    public static SwPlayer Primary => _Primary ??= new();
    public static SwPlayer Secondary => _Secondary ??= new();
    public override int RenderLayer => 2;
    public static byte TypeId => 0;
    protected override byte GetTypeId => TypeId;
    public override uint Mask => 3;
    public double ChargeTime => 1;
    public double ChargeSpeedMul => 0.5;
    // Note: dodge animations run at 12 fps, so 3/12 is 0.25 seconds
    public double DodgeInvulnDelay => 3.0 / 12;
    public double DodgeInvulnWindow => 4.0 / 12;
    public double DodgeDuration => 9.0 / 12;
    public double DodgeCooldown => 0.15;
    public double DodgeSpeedMul => 1.5;
    public double BulletSpeed => 100;
    public int Ammo = 4;
    protected override int NumClocks => base.NumClocks + 3;
    public double Clock0{get => Clocks[base.NumClocks+0]; set {Clocks[base.NumClocks+0] = value;}}
    public double DodgeCooldownClock{get => Clocks[base.NumClocks+1]; set {Clocks[base.NumClocks+1] = value;}}
    public double AttackCooldownClock{get => Clocks[base.NumClocks+2]; set {Clocks[base.NumClocks+2] = value;}}
    private readonly SwStateMachine StateMachine;
    private readonly SwPlayerControls Controls;
    public SwPlayer()
    {
        Controls = new SwPlayerControls(this);
        RegisterComponent(Controls);
        RegisterComponent(new SwInventoryComponent(this, "inventory"));
        if(!SwApp.TryLoadPrion("game_data/particles/particles.json", out var animData)) throw new("bad");
        SwAnimation.TryFromPriAse(out var animation, "dust_1", "game_data/particles", animData);
        SwParticleComponent particles = new(this, "dust_1", animation)
        {
            Offset = new(0, 9)
        };
        RegisterComponent(particles);
        string path = "game_data/entities/actors/player/player_anim_data.json";
        if(!TryLoadSprites(path)) ErEngine.LogWarning("failed to load player sprites");
        SwAreaComponent spoonHurtbox = new(this, "spoon_hurtbox", 4, new(32,32));
        spoonHurtbox.Area.OnBodyEnterFn = OnEnterSpoonHurtbox;
        RegisterComponent(spoonHurtbox);
        StateMachine = SwPlayerState.GetStateMachine(this, "state_machine");
        RegisterComponent(StateMachine);
    }
    private static void SetHud(string key, double value)
    {
        PriDict dict = [];
        dict.TrySet("verb", "hud_set");
        dict.TrySet("key", key);
        dict.TrySet("value", value);
        SwApp.CommandStore.AddGlobalCommand(dict);
    }
    private static void OnEnterSpoonHurtbox(SwColliderArea area, int bodyId, ErColliderBody body)
    {
        if(!SwGame.TryGetEntProps(area.ParentId, out var playerProps)) return;
        if(!SwGame.TryGetEntProps(body.ParentId, out var targetProps)) return;
        if(!playerProps.Props.TryGet("spoon_damage", out PriNode spoonDamage)) return;
        targetProps.AddCommand(spoonDamage);
    }
    public override void Ready()
    {
        base.Ready();
        SwDamage spoonDamage = new([(SwDamageType.Untyped, 10)]);
        EntProps.Props.TrySet("spoon_damage", spoonDamage.ToPri());
    }
    public override void Update()
    {
        base.Update();
        if(DodgeCooldownClock > 0) DodgeCooldownClock -= SwGame.DeltaTime;
        if(AttackCooldownClock > 0) AttackCooldownClock -= SwGame.DeltaTime;
        SwGame.SetPlayerPos(Position);
        EntProps.Props.TrySet("spoon_damage/source_pos_x", Position.X);
        EntProps.Props.TrySet("spoon_damage/source_pos_y", Position.Y);
    }
    protected override void DrawImplLate(SwEntity nextState)
    {
        base.DrawImplLate(nextState);
    }
    protected override double Damage(SwDamage damage)
    {
        double value = base.Damage(damage);
        if(value > 0)
        {
            SetHud("health", Health);
        }
        return value;
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
}