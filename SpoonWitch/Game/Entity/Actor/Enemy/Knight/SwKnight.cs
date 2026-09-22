using Eris;
using ErisPhysics2D.Collider;
using Prion.Node;
using SpoonWitch.ByteStream;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Game.Map.Collision;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Knight;

public class SwKnight : SwEnemy
{
    private readonly SwStateMachine StateMachine;
    public double WanderSpeedMul = 0.25;
    public double TimeoutClock;
    public SwKnight()
    {
        // string path = "game_data/entities/actors/knight/knight_anim_data.json";
        // if(!TryLoadSprites(path)) ErEngine.LogWarning("failed to load knight sprites");
        SwAreaComponent hurtbox = new(this, "hurtbox", 2, new(32,32), onBodyEnter: OnEnterHurtbox);
        RegisterComponent(hurtbox);
        StateMachine = SwKnightState.GetStateMachine(this, "state_machine");
        RegisterComponent(StateMachine);
    }
    public override void Ready()
    {
        base.Ready();
        if(!IsPassive) StateMachine.SetState("wandering");
        SwDamage damage = new([(SwDamageType.Untyped, 30)]);
        Props.TrySet("damage", damage.ToPri());
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
        // if(!SwGame.TryGetEntProps(area.ParentId, out var sourceProps)) return;
        // if(!SwGame.TryGetEntProps(body.ParentId, out var targetProps)) return;
        if(!Props.TryGet("damage", out PriNode damage)) return;
        entity.AddCommand(damage);
    }
}