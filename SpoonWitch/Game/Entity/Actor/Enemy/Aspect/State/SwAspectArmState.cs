using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.Ik;
using SpoonWitch.Game.Entity.Component.Ik.AspectLegs;
using SpoonWitch.Game.Entity.Component.Ik.Tentacle;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State;

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
    protected SwSprite BodySprite = null!;
    protected SwTentacleComponent RightArm = null!;
    protected SwTentacleComponent LeftArm = null!;
    protected SwAspectLegsComponent Legs = null!;
    public override void Ready()
    {
        base.Ready();
        BodySprite = Entity.GetComponent<SwSpriteComponent>("body")?.Sprite!;
        RightArm = Entity.GetComponent<SwTentacleComponent>("right_arm")!;
        LeftArm = Entity.GetComponent<SwTentacleComponent>("left_arm")!;
        Legs = Entity.GetComponent<SwAspectLegsComponent>("legs")!;
    }
    public class SwArmAsleep : SwAspectArmState
    {
        public override string Name => "asleep";
    }
    public static SwArmStateMachine GetStateMachine(SwAspect parent, string name, SwTentacleComponent arm)
    {
        return new(parent, name, arm, [
            new SwAspectAsleep(),
            new SwAspectWake(),
            new SwAspectDead(),
            new SwAspectStationary(),
            new SwAspectWandering(),
            new SwAspectUproot(),
        ]);
    }
}

public class SwArmStateMachine : SwStateMachine<SwAspect>
{
    public readonly SwTentacleComponent Arm;
    public SwArmStateMachine(SwAspect parent, string name, SwTentacleComponent arm, IEnumerable<SwState<SwAspect>> states) : base(parent, name, states)
    {
        Arm = arm;
    }
}
