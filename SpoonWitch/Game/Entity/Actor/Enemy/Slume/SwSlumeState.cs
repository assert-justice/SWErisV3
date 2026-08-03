using Eris;
using Eris.Utils;
using ErisMath;
using SpoonWitch.ByteStream;
using SpoonWitch.Game.Entity.Component.Sprite;
using SpoonWitch.Game.Entity.Component.State;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Slume;

public abstract class SwSlumeState(SwSlume parent) : SwState(parent)
{
    protected readonly SwSlume Slume = parent;
    protected ErWrapper<SwSprite> Sprite = new(() => parent.GetComponent<SwSprite>("body")!);
    protected ErWrapper<SwStateMachine> StateMachine = new(() => parent.GetComponent<SwStateMachine>("state_machine")!);
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        // ErEngine.Log(Name);
    }
    private class Default(SwSlume parent) : SwSlumeState(parent)
    {
        public override string Name => "default";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Sprite.Value.Play("idle_d");
        }
        public override void Update()
        {
            base.Update();
            if(Slume.CanSeePlayer())StateMachine.Value.SetState("chasing");
        }
    }
    private class Chasing(SwSlume parent) : SwSlumeState(parent)
    {
        public override string Name => "chasing";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Sprite.Value.Play("move_d");
        }
        public override void Update()
        {
            base.Update();
            Slume.TargetPosition = SwGame.PlayerPos;
            if(!Slume.CanSeePlayer())StateMachine.Value.SetState("seeking");
            Slume.MoveToTarget(Slume.BaseSpeed);
        }
    }
    private class Seeking(SwSlume parent) : SwSlumeState(parent)
    {
        public override string Name => "seeking";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Sprite.Value.Play("move_d");
            Slume.TimeoutClock = 1;
        }
        public override void Update()
        {
            base.Update();
            if(Slume.CanSeePlayer())StateMachine.Value.SetState("chasing");
            else if(Slume.TimeoutClock > 0) Slume.TimeoutClock -= SwGame.DeltaTime;
            else StateMachine.Value.SetState("wandering");
            Slume.MoveToTarget(Slume.BaseSpeed);
        }
    }
    private class Wandering(SwSlume parent) : SwSlumeState(parent)
    {
        public override string Name => "wandering";
        private bool TryRandomTarget()
        {
            // Todo: optimize this
            double angle = Random.Shared.NextDouble() * ErMath.TAU;
            var dir = ErVec2.FromAngle(angle) * 128;
            var pos = dir + Slume.Position;
            if(!Slume.CanSeePoint(pos)) return false;
            if(!SwGame.GetMap().TryGetRoom(pos, out var targetRoom)) return false;
            if(!SwGame.GetMap().TryGetRoom(Slume.Position, out var room)) return false;
            if(targetRoom.Id != room.Id) return false;
            Slume.TargetPosition = pos;
            Slume.TimeoutClock = 1;
            return true;
        }
        private void SetNewWander()
        {
            for (int i = 0; i < 10; i++)
            {
                if(TryRandomTarget()) return;
            }
            ErEngine.LogWarning("slume could not find target pos");
        }
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            // Slume.TargetPosition = SwGame.PlayerPos;
            Sprite.Value.Play("move_d");
            SetNewWander();
            // ErEngine.Log("wandering");
            // Pick random wander point
        }
        public override void Update()
        {
            base.Update();
            if(Slume.CanSeePlayer())StateMachine.Value.SetState("chasing");
            // else if(Slume.DistanceToTarget() < 64)SetNewWander();
            else if(Slume.TimeoutClock > 0)
            {
                Slume.TimeoutClock -= SwGame.DeltaTime;
                Slume.MoveToTarget(Slume.BaseSpeed * Slume.WanderSpeedMul);
            }
            // else if wander point is far away, move towards it
            else
            {
                // pick a new random wander point
                SetNewWander();
            }
        }
    }
    public static SwStateMachine GetStateMachine(SwSlume parent, string name)
    {
        return new(parent, name, [
            new Default(parent),
            new Chasing(parent),
            new Seeking(parent),
            new Wandering(parent),
        ]);
    }
}