using Eris;
using ErisMath;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.Ik;
using SpoonWitch.Game.Entity.Component.Ik.AspectLegs;
using SpoonWitch.Game.Entity.Component.Ik.Tentacle;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Rendering;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State.ArmState;

public abstract class SwAspectArmState: SwState<SwAspect>
{
    /*
    States needed:
    Asleep
    Wake
    Wandering
    Attacking
    Recoiling
    Dying
    Dead
    */
    protected SwTentacleComponent Arm = null!;
    protected SwArmStateMachine StateM = null!;
    // {
    //     get
    //     {
    //         if(StateMachine is SwArmStateMachine armState) return armState.Arm;
    //         return null!;
    //     }
    // }
    protected SwClock Clock{get; private set;} = null!;
    public SwAspectArmState()
    {
    }
    public override void Ready()
    {
        base.Ready();
        Clock = Entity.AddClock();
        if(StateMachine is SwArmStateMachine armState) StateM = armState;
        Arm = StateM.Arm;
        // BodySprite = Entity.GetComponent<SwSpriteComponent>("body")?.Sprite!;
        // RightArm = Entity.GetComponent<SwTentacleComponent>("right_arm")!;
        // LeftArm = Entity.GetComponent<SwTentacleComponent>("left_arm")!;
        // Legs = Entity.GetComponent<SwAspectLegsComponent>("legs")!;
    }
    // public override void BeginState(string lastState)
    // {
    //     base.BeginState(lastState);
    //     ErEngine.Log(Name);
    // }
    public override void Update(double dt)
    {
        base.Update(dt);
        StateM.Hurtbox.Offset = Arm.TipPos - Entity.Position;
    }
    // public class SwArmAsleep : SwAspectArmState
    // {
    //     public override string Name => "asleep";
    // }
    public static SwArmStateMachine GetStateMachine(SwAspect parent, string name, SwTentacleComponent arm)
    {
        return new(parent, name, arm, [
            new SwArmAsleep(),
            new SwArmAwake(),
            new SwArmAttacking(),
            new SwArmCooldown(),
            // new SwAspectAsleep(),
            // new SwAspectWake(),
            // new SwAspectDead(),
            // new SwAspectStationary(),
            // new SwAspectWandering(),
            // new SwAspectUproot(),
        ]);
    }
}

public class SwArmStateMachine : SwStateMachine<SwAspect>
{
    public readonly SwTentacleComponent Arm;
    public ErRect2 WanderRect;
    public ErRect2 AttackRect;
    public SwAreaComponent VisionArea = null!;
    public SwHurtboxComponent Hurtbox = null!;
    public bool Tracking = false;
    public SwArmStateMachine(SwAspect parent, string name, SwTentacleComponent arm, IEnumerable<SwState<SwAspect>> states) : base(parent, name, states)
    {
        Arm = arm;
    }
}
