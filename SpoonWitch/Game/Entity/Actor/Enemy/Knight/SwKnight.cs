using Eris;
using Prion.Node;
using SpoonWitch.Game.Effect;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Knight;

public class SwKnight : SwEnemy
{
    private SwStateMachine<SwKnight> StateMachine = null!;
    public double WanderSpeedMul = 0.5;
    public double HurtDelay = 0.375;
    public double HurtDuration = 0.125;
    public double Cooldown = 0.5;
    public SwClock TimeoutClock;
    public SwKnight()
    {
        TimeoutClock = AddClock();
    }
    public override void SetProps(PriNode props)
    {
        base.SetProps(props);
        if(Props.TryGet("speed/wander_speed_mul", out double d)) WanderSpeedMul = d;
        if(Props.TryGet("sword/hurt_delay", out d)) HurtDelay = d;
        if(Props.TryGet("sword/hurt_duration", out d)) HurtDuration = d;
        if(Props.TryGet("sword/cooldown", out d)) Cooldown = d;
    }
    public override void Init()
    {
        base.Init();
        LoadSprites("anim_data/sprites");
        if(!Props.TryGet("hurtbox/mask", out uint mask)) mask = (uint)SwCollisionMask.PlayerTeam;
        var hurtboxSize = SwPrion.GetVec2(Props.Get("hurtbox/size"), defaultVec: new(32, 32));
        SwAreaComponent hurtbox = new(this, "hurtbox", mask, hurtboxSize, onBodyEnter: OnEnterHurtbox);
        RegisterComponent(hurtbox);
        StateMachine = SwKnightState.GetStateMachine(this, "state_machine");
        RegisterComponent(StateMachine);
    }
    public override void Ready()
    {
        base.Ready();
        if(!IsPassive) StateMachine.SetState("wandering");
    }
    protected override void Die()
    {
        base.Die();
        StateMachine.SetState("dead");
    }
    protected override double Damage(SwDamage damage)
    {
        double value = base.Damage(damage);
        if(value > 0) StateMachine.SetState("knockback");
        return value;
    }
    private void OnEnterHurtbox(SwEntity entity)
    {
        if(!Props.TryGet("damage", out PriNode damage)) return;
        entity.AddCommand(damage);
    }
    public override void GetMad()
    {
        base.GetMad();
        StateMachine.SetState("wandering");
    }
}