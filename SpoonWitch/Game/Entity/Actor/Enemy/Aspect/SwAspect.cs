using Eris;
using Prion.Node;
using SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State;
using SpoonWitch.Game.Entity.Component.Ik;
using SpoonWitch.Game.Entity.Component.Ik.Tentacle;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect;

public class SwAspect: SwEnemy
{
    public double BaseArmSpeed = 100;
    public double WanderSpeedMul = 0.5;
    public double WanderRadius = 128;
    public readonly SwClock TimeoutClock;
    private SwStateMachine<SwAspect> StateMachine = null!;
    public SwAspect()
    {
        AddGlobalHandler("boss_wake", Wake);
        TimeoutClock = AddClock();
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
        // StateMachine.SetState("wake");
    }
}
