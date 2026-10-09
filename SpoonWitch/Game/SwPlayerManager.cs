using ErisMath;
using SpoonWitch.Game.Entity.Actor.Player;

namespace SpoonWitch.Game;

public class SwPlayerManager
{
    public readonly SwGame Game;
    public int NumPlayers{get; private set;}
    public int NumLivingPlayers{get; private set;}
    public int NumPlayerInstances{get; private set;}
    private enum PmState
    {
        Normal,
        Despawn,
        FadeOut,
        Respawn,
    }
    private PmState State = PmState.FadeOut;
    public SwPlayerManager(SwGame game)
    {
        Game = game;
    }
    public void SetNumPlayers(int numPlayers)
    {
        // add or remove players as required
        NumPlayers = numPlayers;
    }
    public void Update()
    {
        switch (State)
        {
            case PmState.Normal:
                UpdateNormal();
                break;
            case PmState.Despawn:
                UpdateDespawn();
                break;
            case PmState.FadeOut:
                UpdateFadeOut();
                break;
            case PmState.Respawn:
                UpdateRespawn();
                break;
        }
    }
    private void UpdateNormal()
    {
        int numLiving = 0;
        foreach (var player in Game.EntityLookup.GetValues<SwPlayer>())
        {
            if(player.IsAlive) numLiving++;
        }
        if(numLiving == 0 && NumLivingPlayers > 0)
        {
            RespawnPlayers();
        }
        NumLivingPlayers = numLiving;
    }
    private void UpdateDespawn()
    {
        bool done = true;
        foreach (var player in Game.EntityLookup.GetValues<SwPlayer>())
        {
            if(Game.IsRectVisibleToAny(player.Rect)) done = false;
            else player.RespawnSpeedMul = 0;
        }
        if(!done) return;
        SwApp.CommandQueue.AddCommandVerb("game_fade_out");
        State = PmState.FadeOut;
    }
    private void UpdateFadeOut()
    {
        if(Game.FadeOpacity < 1) return;
        var checkpointPos = Game.Map.CurrentCheckpointPos;
        State = PmState.Respawn;
        Game.SnapToTarget(checkpointPos);
        // snap players to a given distance
        foreach (var player in Game.EntityLookup.GetValues<SwPlayer>())
        {
            player.RespawnSpeedMul = 1;
            ErVec2 diff = player.Position - checkpointPos;
            double distance = 400;
            ErVec2 offset = diff.Normalized() * distance;
            ErVec2 pos = checkpointPos + offset;
            player.Position = pos;
        }
    }
    private void UpdateRespawn()
    {
        foreach (var player in Game.EntityLookup.GetValues<SwPlayer>())
        {
            if (player.IsAlive)
            {
                State = PmState.Normal;
                break;
            }
            // if(Game.FadeOpacity == 1 && Game.IsRectVisibleToAny(player.Rect)) SwApp.CommandQueue.AddCommandVerb("game_fade_in");
            if(Game.FadeOpacity == 1) SwApp.CommandQueue.AddCommandVerb("game_fade_in");
        }
    }
    private void RespawnPlayers()
    {
        SwApp.CommandQueue.AddCommandVerb("player_respawn");
        // if the checkpoint is not in the current room, start despawn and fade out
        // otherwise skip to respawn
        if(!Game.Map.IsPointInRoom(Game.Map.CurrentRoom?.Data.Iid ?? string.Empty, Game.Map.CurrentCheckpointPos))
        {
            State = PmState.Despawn;
        }
        else State = PmState.Respawn;
    }
}
