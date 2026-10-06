using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.Ik;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State;

public abstract class SwAspectState: SwState<SwAspect>
{
    /*
    States needed:
    Asleep
    Wake
    Stationary
    BeginPhase2
    RangedAttack
    MeleeAttack
    Chasing
    Wandering
    Resting (taking a breather after damage thresholds)
    Dead
    */
    protected SwSprite BodySprite = null!;
    protected SwTentacleComponent RightArm = null!;
    protected SwTentacleComponent LeftArm = null!;
    public override void Ready()
    {
        base.Ready();
        BodySprite = Entity.GetComponent<SwSpriteComponent>("body")?.Sprite!;
        RightArm = Entity.GetComponent<SwTentacleComponent>("right_arm")!;
        LeftArm = Entity.GetComponent<SwTentacleComponent>("left_arm")!;
    }
    public static SwStateMachine<SwAspect> GetStateMachine(SwAspect parent, string name)
    {
        return new(parent, name, [
            new SwAspectAsleep(),
            new SwAspectWake(),
            new SwAspectDead(),
            new SwAspectStationary(),
            new SwAspectWandering(),
        ]);
    }
}
