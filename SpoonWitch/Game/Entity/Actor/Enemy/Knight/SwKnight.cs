using Eris;
using Prion.Node;
using SpoonWitch.Game.Effect;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.State;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Knight;

public class SwKnight : SwEnemy
{
    private SwStateMachine? StateMachine;
    public double WanderSpeedMul = 0.25;
    public double TimeoutClock;
    protected override void SetProps(PriNode props)
    {
        base.SetProps(props);
        LoadSprites("anim_data/sprites");
        SwAreaComponent hurtbox = new(this, "hurtbox", 2, new(32,32), onBodyEnter: OnEnterHurtbox);
        RegisterComponent(hurtbox);
        StateMachine = SwKnightState.GetStateMachine(this, "state_machine");
        RegisterComponent(StateMachine);
        if(!IsPassive) StateMachine.SetState("wandering");
        SwDamage damage = new([(SwDamageType.Untyped, 30)]);
        Props.TrySet("damage", damage.ToPri());
    }
    protected override void Die()
    {
        base.Die();
        StateMachine?.SetState("dead");
    }
    protected override double Damage(SwDamage damage)
    {
        double value = base.Damage(damage);
        if(value > 0) StateMachine?.SetState("knockback");
        return value;
    }
    private void OnEnterHurtbox(SwEntity entity)
    {
        if(!Props.TryGet("damage", out PriNode damage)) return;
        entity.AddCommand(damage);
    }
}