using Eris;
using ErisMath;

namespace SpoonWitch.Game.Entity.Actor.Player.PlayerState;

public class SwPlayerRespawnFadeOut: SwPlayerState
{
    public override string Name => "respawn_fade_out";
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        PlayBodyAnim("fly");
        SwApp.CommandQueue.AddCommandVerb("game_fade_out");
        // SwGame.Game.FadeOut();
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        Entity.MoveToward(Entity.Game.Map.CurrentCheckpointPos, Entity.BaseSpeed);
        bool isVisible = Entity.Game.IsRectVisibleToAny(ErRect2.Centered(Entity.Position,Entity.Size));
        // bool isVisible = SwGame.Camera.IsPointVisible(Entity.Position);
        if (isVisible)
        {
            Entity.MoveToward(Entity.Game.Map.CurrentCheckpointPos, Entity.BaseSpeed);
        }
        else Entity.Velocity = ErVec2.Zero;
        if(Entity.Game.FadeOpacity == 1 && !isVisible) StateMachine.SetState("respawn_fade_in");
        // if(Entity.Game.FadeState == 1 && !isVisible) StateMachine.SetState("respawn_fade_in");
    }
}
// public class SwPlayerRespawnQuick: SwPlayerState
// {
//     public override string Name => "quick_spawn";
//     public override void BeginState(string lastState)
//     {
//         base.BeginState(lastState);
//         SwGame.SetCameraTarget(SwGame.ActiveCheckpoint.RectPx.Center, true);
//         Entity.Position = SwGame.ActiveCheckpoint.RectPx.Center;
//     }
//     public override void Update()
//     {
//         base.Update();
//         Entity.IsAlive = true;
//         StateMachine.SetState("default");
//     }
// }
public class SwPlayerRespawnFadeIn: SwPlayerState
{
    public override string Name => "respawn_fade_in";
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        PlayBodyAnim("fly");
        SwApp.CommandQueue.AddCommandVerb("game_fade_in");
        ErVec2 checkpointPos = Entity.Game.Map.CurrentCheckpointPos;
        Entity.Game.SnapToTarget(checkpointPos);
        ErVec2 diff = Entity.Position - checkpointPos;
        double distance = diff.GetLength();
        if(300 < distance) distance = 300;
        ErVec2 offset = diff.Normalized() * distance;
        ErVec2 pos = checkpointPos + offset;
        Entity.Position = pos;
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        if(BodySprite.CurrentAnimation.Name == "respawn")
        {
            if (!BodySprite.IsPlaying)
            {
                StateMachine.SetState("default");
                Entity.IsAlive = true;
            }
            return;
        }
        double distance = Entity.MoveToward(Entity.Game.Map.CurrentCheckpointPos, Entity.BaseSpeed, dt);
        if(distance == 0)
        {
            PlayBodyAnim("respawn");
        }
    }
}
public class SwPlayerRespawn: SwPlayerState
{
    public override string Name => "respawn";
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        PlayBodyAnim("fly");
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        ErVec2 checkpointPos = Entity.Game.Map.CurrentCheckpointPos;
        Entity.Game.SnapToTarget(checkpointPos);
        if(BodySprite.CurrentAnimation.Name == "respawn")
        {
            if(!BodySprite.IsPlaying) StateMachine.SetState("default");
            Entity.IsAlive = true;
            return;
        }
        if(Entity.MoveToward(Entity.Game.Map.CurrentCheckpointPos, Entity.BaseSpeed, dt) == 0) PlayBodyAnim("respawn");
        // ErVec2 diff = SwGame.ActiveCheckpoint.RectPx.Center - Entity.Position;
        // double speed = Entity.BaseSpeed * SwGame.DeltaTime;
        // double lenSq = diff.GetLengthSquared();
        // if(lenSq < speed * speed)
        // {
        //     Entity.Velocity = ErVec2.Zero;
        //     PlayBodyAnim("respawn");
        // }
        // else
        // {
        //     ErVec2 dir = (Entity.Game.Map.CurrentCheckpointPos - Entity.Position).Normalized();
        //     Entity.Velocity = dir * Entity.BaseSpeed;
        // }
    }
}
