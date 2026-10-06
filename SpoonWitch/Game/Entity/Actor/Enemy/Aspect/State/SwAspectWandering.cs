using Eris;
using ErisMath;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State;

public class SwAspectWandering : SwAspectState
{
    public override string Name => "wandering";
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        BodySprite.Play("phase2_idle");
        LeftArm.TipSpeed = Entity.BaseArmSpeed;
        LeftArm.IsActive = true;
        LeftArm.IsVisible = true;
        RightArm.TipSpeed = Entity.BaseArmSpeed;
        RightArm.IsActive = true;
        RightArm.IsVisible = true;
        SetNewWander();
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        if(LeftArm.IsAtTarget) LeftArm.Target = SwRandom.GetRandomPointOnCircle(new ErVec2(-64,-32),48);
        if(RightArm.IsAtTarget) RightArm.Target = SwRandom.GetRandomPointOnCircle(new ErVec2(64,-32),48);
        if(Entity.TimeoutClock.IsRunning)
        {
            Entity.MoveToTarget(Entity.BaseSpeed * Entity.WanderSpeedMul);
        }
        else
        {
            SetNewWander();
        }
    }
    private bool TryRandomTarget()
    {
        ErVec2 targetPos = SwRandom.GetRandomPointOnCircle(Entity.Position, Entity.WanderRadius);
        if(!Entity.CanSeePoint(targetPos)) return false;
        if(!Entity.Game.Map.InSameRoom(Entity.Position, targetPos)) return false;
        Entity.TargetPosition = targetPos;
        Entity.TimeoutClock.Start(1);
        return true;
    }
    private void SetNewWander()
    {
        for (int i = 0; i < 50; i++)
        {
            if(TryRandomTarget()) return;
        }
        ErEngine.LogWarning("aspect could not find target pos");
    }

}
