using Eris;
using Prion.Node;
using SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State;
using SpoonWitch.Game.Entity.Component.Ik;
using SpoonWitch.Game.Entity.Component.State;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect;

public class SwAspect: SwEnemy
{
    private SwStateMachine<SwAspect> StateMachine = null!;
    public SwAspect()
    {
        AddGlobalHandler("boss_wake", Wake);
    }
    public override void SetProps(PriNode props)
    {
        base.SetProps(props);
    }
    public override void Init()
    {
        base.Init();
        StateMachine = RegisterComponent(SwAspectState.GetStateMachine(this, "state_machine"));
        StateMachine.SetDefaultState("stationary");
        RegisterComponent(new SwTentacleComponent(this, "right_arm"));
        RegisterComponent(new SwTentacleComponent(this, "left_arm"));
        LoadSprites("anim_data/sprites");
    }
    private void Wake(PriNode command)
    {
        StateMachine.SetState("wake");
    }
}
