using Eris;
using ErisMath;
using ErisPhysics2D.Collider;
using Prion.Node;
using SpoonWitch.ByteStream;
using SpoonWitch.Game.Effect;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Game.Map.Collision;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Slume;

public class SwSlume : SwEnemy
{
    public ErVec2 HurtboxSize = new(20, 20);
    public double TimeoutClock;
    private SwStateMachine? StateMachine;
    // public double BaseSpeed = 100;
    public double WanderSpeedMul = 0.5;
    // public double MaxHealth = 100;
    protected override void SetProps(PriNode props)
    {
        base.SetProps(props);
        BaseSpeed = 50;
        SwAreaComponent hurtbox = new(this, "hurtbox", 2, new(18, 18), onBodyEnter: OnEnterHurtbox);
        RegisterComponent(hurtbox);
        StateMachine = SwSlumeState.GetStateMachine(this, "state_machine");
        RegisterComponent(StateMachine);
        if(!IsPassive) StateMachine?.SetState("wandering");
        SwDamage damage = new([(SwDamageType.Untyped, 10)]);
        Props.TrySet("damage", damage.ToPri());
        LoadSprites("anim_data/sprites");
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
    public override void GetMad()
    {
        base.GetMad();
        StateMachine?.SetState("fleeing");
    }
}