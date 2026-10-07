using Eris;
using ErisMath;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State;

public class SwAspectWandering : SwAspectState
{
    public override string Name => "wandering";
    private ErVec2 Home;
    private readonly double Tether = 140;
    private double LeftArmDelay;
    private double RightArmDelay;
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        BodySprite.Play("phase2_idle");
        Home = Entity.Position;
        LeftArm.TipSpeed = Entity.BaseArmSpeed;
        LeftArm.Activate();
        RightArm.TipSpeed = Entity.BaseArmSpeed;
        RightArm.Activate();
        LeftArmDelay = Random.Shared.NextDouble();
        RightArmDelay = LeftArmDelay + 0.5;
        if(RightArmDelay > 1)RightArmDelay-=1;
        Entity.LeftArmClock.Start(LeftArmDelay);
        Entity.RightArmClock.Start(RightArmDelay);
        Legs.Activate();
        Legs.Randomize();
        SetNewWander();
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        Legs.Velocity = Entity.Velocity;
        ErVec2 target;
        if (!Entity.LeftArmClock.IsRunning)
        {
            target = Entity.Position + SwRandom.GetRandomPointOnCircle(new ErVec2(64,-64),48);
            LeftArm.SetTarget(target);
            Entity.LeftArmClock.Start(0.5);
        }
        if (!Entity.RightArmClock.IsRunning)
        {
            target = Entity.Position + SwRandom.GetRandomPointOnCircle(new ErVec2(-64,-64),48);
            RightArm.SetTarget(target);
            Entity.RightArmClock.Start(0.5);
        }
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
        if((targetPos-Home).GetLengthSquared() > Tether * Tether) return false;
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
