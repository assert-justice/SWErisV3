using Eris;
using Eris.Renderer;
using ErisMath;
using ErisPhysics2D;
using Prion.Node;
using SpoonWitch.Data;
using SpoonWitch.Game.Entity;
using SpoonWitch.Game.Entity.Actor.Player;
using SpoonWitch.Game.Map;
using SpoonWitch.Game.Map.MapData;
using SpoonWitch.UI.Hud;
using SpoonWitch.Utils;

namespace SpoonWitch.Game;

public class SwGame
{
    // Tilemap stuff
    public readonly SwMap Map;
    // Rendering stuff
    private readonly ErTexture[] RenderTextures;
    private int RenderLayerIdx;
    public void SetRenderLayer(int renderLayerIdx)
    {
        if(renderLayerIdx == RenderLayerIdx) return;
        ErEngine.Renderer.PopViewport();
        RenderLayerIdx = renderLayerIdx;
        ErEngine.Renderer.PushViewport(CurrentCamera?.Rect.Position ?? ErVec2.Zero, RenderTextures[RenderLayerIdx]);
    }
    private readonly SwCamera[] Cameras;
    private SwCamera? CurrentCamera{get; set;}
        private readonly List<ErVec2> FocusPoints = [];
    public void ClearFocusPoints()
    {
        FocusPoints.Clear();
    }
    public void AddFocusPoint(ErVec2 point)
    {
        FocusPoints.Add(point);
    }
    public bool IsPointVisibleToAll(ErVec2 point)
    {
        foreach (var camera in Cameras)
        {
            if(!camera.IsPointVisible(point)) return false;
        }
        return true;
    }
    public bool IsPointVisibleToAny(ErVec2 point)
    {
        foreach (var camera in Cameras)
        {
            if(camera.IsPointVisible(point)) return true;
        }
        return false;
    }
    public double MaxCameraDistance(ErVec2 point)
    {
        double res = double.MinValue;
        foreach (var camera in Cameras)
        {
            double d = camera.Rect.DistanceToPoint(point);
            if(d > res) res = d;
        }
        return res;
    }
    public ErVec2 CameraTarget{get; set;}
    // Physics stuff
    public readonly ErPhysicsWorld2D PhysicsWorld;
    // Entity stuff
    private readonly SwHud[] Huds;
    public readonly SwLookup EntityLookup = new();
    private readonly Queue<SwEntity> NewEntities = [];
    private readonly Queue<SwEntity> FreedEntitiesQueue = [];
    public SwGame(SwMapData mapData, PriNode launchProps)
    {
        if(!launchProps.TryGet("num_players", out int numPlayers)) numPlayers = 1;
        bool skipKb = launchProps.TryGet("skip_kb", out bool b) && b;
        PhysicsWorld = new(new(8,8), mapData.TileData.TileSize);
        Map = new(this, mapData)
        {
            OnNewCurrentRoom = HandleNewRoom,
        };
        // Todo: support more cameras
        Cameras = [new()];
        RenderTextures = new ErTexture[5];
        for (int idx = 0; idx < RenderTextures.Length; idx++)
        {
            RenderTextures[idx] = ErTexture.GetRenderTexture((int)SwApp.CameraSize.X, (int)SwApp.CameraSize.Y);
        }
        CameraTarget = Map.CurrentCheckpointPos;
        // add players
        Huds = new SwHud[numPlayers];
        var playerProps = SwData.Prototypes.Get("entities/player");
        // Todo: make assigning input devices better
        var inputBinds = SwData.Settings.Get("input_binds");
        List<(int type, int gamepadIdx)> inputBinders = [];
        if(numPlayers == 1) inputBinders.Add((0,0));
        else
        {
            int count = numPlayers;
            if(!skipKb)
            {
                inputBinders.Add((1,0));
                count--;
            }
            for (int idx = 0; idx < count; idx++)
            {
                inputBinders.Add((2,idx));
            }
        }
        double playerWidth = 48;
        double startX = CameraTarget.X - numPlayers * playerWidth / 2 + playerWidth / 2;
        for (int idx = 0; idx < numPlayers; idx++)
        {
            var player = AddEntity<SwPlayer>(playerProps);
            player.PlayerIdx = idx;
            player.Camera = Cameras[0];
            player.Position = new(startX + playerWidth * idx, CameraTarget.Y);
            var (type,gamepadIdx) = inputBinders[idx];
            switch (type)
            {
                case 0:
                player.Controls.InputDevice.SetProfileAll(inputBinds);
                break;
                case 1:
                player.Controls.InputDevice.SetProfileKbm(inputBinds);
                break;
                case 2:
                player.Controls.InputDevice.SetProfileGamepad(inputBinds, gamepadIdx);
                break;
            }
            // if(idx < inputBinders.Count) inputBinders[idx](player, inputBinds);
            int hudX = SwApp.INTERNAL_WIDTH / 2 * idx;
            if(!SwHud.TryLoad(new(hudX, 0), out var hud)){}
            Huds[idx] = hud;
            hud.Player = player; 
        }
    }
    public void Update(double dt)
    {
        // Clear points of interest
        FocusPoints.Clear();
        // Update entities
        while(NewEntities.TryDequeue(out var newEntity))
        {
            if(EntityLookup.TryAdd(newEntity.Id.ToString(), newEntity)) newEntity.Ready();
            else ErEngine.LogWarning("bad id for entity, id: ", newEntity.Id, " type: ", newEntity.GetType());
        }
        foreach (var entity in EntityLookup.GetAllValues<SwEntity>())
        {
            entity.GameUpdate(dt);
            if(entity.IsFreeQueued) FreedEntitiesQueue.Enqueue(entity);
        }
        FreeEntities();
        // Handle player room transitions
        // Todo: make this better
        HashSet<string> roomsRequested = [];
        List<(string playerId, string roomId)> temp = [];
        foreach (var player in EntityLookup.GetValues<SwPlayer>())
        {
            var targetPoint = player.Position + player.Velocity * dt;
            if (MaxCameraDistance(targetPoint) > 8)
            {
                player.Velocity = ErVec2.Zero;
                targetPoint = player.Position;
            }
            if(!Map.TryGetRoomId(targetPoint, out string roomId)) continue;
            roomsRequested.Add(roomId);
            temp.Add((player.Id.ToString(), roomId));
        }
        // if more than one room is requested, cancel the velocity of all players trying to leave the current room
        if(roomsRequested.Count > 1)
        {
            foreach (var (playerId,roomId) in temp)
            {
                if(roomId == (Map.CurrentRoom?.Data.Iid ?? string.Empty))continue;
                if(EntityLookup.TryGet<SwPlayer>(playerId, out var player)) player.Velocity = ErVec2.Zero;
            }
        }
        // Update hud
        foreach (var hud in Huds)
        {
            hud.Update(dt);
        }
        // Update areas
        PhysicsWorld.UpdateAreas();
        // Get focus point
        // target pos = average of focus points
        if(FocusPoints.Count > 0)
        {
            ErVec2 pos = ErVec2.Zero;
            foreach (var item in FocusPoints)
            {
                pos += item;
            }
            CameraTarget = pos / FocusPoints.Count;
        }
        // Handle room stuff
        Map.Update(CameraTarget);
        // Update cameras
        foreach (var camera in Cameras)
        {
            camera.SetTargetPosition(CameraTarget);
            camera.Update(dt);
        }
        // Handle commands
    }
    private void FreeEntities()
    {
        while(FreedEntitiesQueue.TryDequeue(out var entity))
        {
            entity.GameCleanup();
            EntityLookup.Remove(entity.Id.ToString());
        }
    }
    private void HandleNewRoom(SwRoom? room, bool wasNull)
    {
        foreach (var camera in Cameras)
        {
            if(room is null)
            {
                camera.UseBounds = false;
                continue;
            }
            camera.UseBounds = true;
            camera.SetBounds((ErRect2)room.RectPx);
            camera.SetTargetPosition(CameraTarget);
            if(wasNull) camera.SnapToTarget();
        }
    }
    public void Draw()
    {
        foreach (var camera in Cameras)
        {
            CurrentCamera = camera;
            // init camera draw
            ErEngine.Renderer.SetClearColor(default);
            camera.BeginDraw();
            // push first render layer
            RenderLayerIdx = 0;
            ErEngine.Renderer.PushViewport(CurrentCamera?.Position ?? ErVec2.Zero, RenderTextures[RenderLayerIdx]);
            // clear render textures
            for (int idx = 0; idx < RenderTextures.Length; idx++)
            {
                SetRenderLayer(idx);
                ErEngine.Renderer.Clear();
            }
            SetRenderLayer(0);
            // draw map
            Map.Draw();
            if(SwData.Settings.TryGet("debug/debug", out bool b) && b) PhysicsWorld.DebugDraw();
            // draw entities
            foreach (var entity in EntityLookup.GetAllValues<SwEntity>())
            {
                entity.GameDraw();
            }
            // draw fade
            // pop render layer
            ErEngine.Renderer.PopViewport();
            // draw render layers
            for (int idx = 0; idx < RenderTextures.Length; idx++)
            {
                RenderTextures[idx].Draw(ErVec2.Zero);
            }
            // end camera draw
            camera.EndDraw();
            // draw huds
            foreach (var hud in Huds)
            {
                hud.Draw();
            }
        }
        CurrentCamera = null;
    }
    public void Cleanup()
    {
        Map.Cleanup();
        foreach (var item in EntityLookup.GetAllValues<SwEntity>())
        {
            FreedEntitiesQueue.Enqueue(item);
        }
        FreeEntities();
    }
    public T AddEntity<T>(PriNode props) where T: SwEntity, new()
    {
        var ent = SwEntity.GameLoad<T>(this, props);
        NewEntities.Enqueue(ent);
        return ent;
    }
}
