using Eris;
using Prion.Node;
using SpoonWitch.Game.Effect;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Slume;

public class SwSlume : SwEnemy
{
    public SwClock TimeoutClock;
    private SwStateMachine<SwSlume> StateMachine = null!;
    public double WanderSpeedMul = 0.5;
    public double FleeThreshold = 0.5;
    public double SeekGiveUpTime = 1;
    public SwSlume()
    {
        TimeoutClock = AddClock();
    }
    public override void SetProps(PriNode props)
    {
        base.SetProps(props);
        if(Props.TryGet("speed/wander_speed_mul", out double d)) WanderSpeedMul = d;
        if(Props.TryGet("state_machine/flee_threshold", out d)) FleeThreshold = d;
        if(Props.TryGet("state_machine/seek_give_up_time", out d)) SeekGiveUpTime = d;
    }
    public override void Init()
    {
        base.Init();
        LoadSprites("anim_data/sprites");
        if(!Props.TryGet("hurtbox/mask", out uint mask)) mask = (uint)SwCollisionMask.PlayerTeam;
        var hurtboxSize = SwPrion.GetVec2(Props.Get("hurtbox/size"), defaultVec: new(18, 18));
        SwAreaComponent hurtbox = new(this, "hurtbox", mask, hurtboxSize, onBodyEnter: OnEnterHurtbox);
        RegisterComponent(hurtbox);
        StateMachine = SwSlumeState.GetStateMachine(this, "state_machine");
        RegisterComponent(StateMachine);
    }
    public override void Ready()
    {
        base.Ready();
        StateMachine.SetDefaultState(IsPassive ? "default" : "wandering");
    }
    protected override void Die()
    {
        base.Die();
        StateMachine?.SetState("dead");
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
        StateMachine.SetState("fleeing");
    }
}