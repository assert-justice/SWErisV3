using System.Text.Json.Nodes;
using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Db;
using Prion.Node;
using Prion.Parser;
using SpoonWitch.ByteStream;
using SpoonWitch.Command;
using SpoonWitch.Data;
using SpoonWitch.Game.Entity;
using SpoonWitch.Game.Entity.Actor.Enemy.Aspect;
using SpoonWitch.Game.Entity.Actor.Enemy.Knight;
using SpoonWitch.Game.Entity.Actor.Enemy.Slume;
using SpoonWitch.Game.Entity.Actor.Player;
using SpoonWitch.Game.Inventory;
using SpoonWitch.Game.Map;
using SpoonWitch.Game.Map.MapObject;
using SpoonWitch.Rendering;
using SpoonWitch.UI.Hud;
using SpoonWitch.Utils;

namespace SpoonWitch.Game;

public class SwGame
{
    public static double DeltaTime => ErEngine.DeltaTime * GameSpeed;
    public static double FrameDuration => ErEngine.FrameDuration * GameSpeed;
    // The factor to blend between the last state and the next state with
    public static double FrameWeight{get; private set;}
    private static ErTexture[] RenderTextures = [];
    public static SwMapCheckpoint ActiveCheckpoint{get; private set;} = null!;
    public static double GameSpeed => SwApp.IsPaused ? 0 : 1;
    private static int _RenderLayer;
    public static int RenderLayer
    {
        get => _RenderLayer;
        set
        {
            if(value == _RenderLayer) return;
            ErEngine.Renderer.PopViewport();
            _RenderLayer = value;
            ErEngine.Renderer.PushViewport(Camera.DrawPos, RenderTextures[value]);
        }
    }
    private SwMap? _Map = null;
    public static SwMap Map => Game._Map!;
    public readonly SwLookup EntityLookup = new();
    private readonly Queue<SwEntity> NewEntities = [];
    private readonly Queue<SwEntity> FreedEntities = [];
    private SwRoom? CurrentRoom;
    private readonly SwHud Hud;
    public double FadeState = 0;
    public double FadeDelta => Math.Sign(FadeTarget - FadeState) / FadeTime * DeltaTime;
    private double FadeTarget = 0;
    private readonly double FadeTime = 1.0;
    private PriNode FadeOnFinishCommand = PriNull.Null;
    private readonly SwCommandHandler CommandHandler = new(SwApp.CommandStore);
    public static readonly SwCamera Camera = new();
    public static ErVec2 PlayerPos{get; private set;}
    public static SwGame Game{get; private set;} = null!;
    public static SwTileData[] TileData{get; private set;} = null!;
    public void Cleanup()
    {
        _Map = null;
        FrameWeight = 0;
        RenderTextures = [];
        CurrentRoom = null;
        Game = null!;
        TileData = null!;
    }
    public static void SetCameraTarget(ErVec2 point, bool shouldSnap = false)
    {
        if(Game.CurrentRoom is null || !Game.CurrentRoom.RectPx.Contains(point))
        {
            if(Map.TryGetRoom(point, out var room))
            {
                shouldSnap = shouldSnap || Game.CurrentRoom is null;
                Game.CurrentRoom = room;
                Camera.UseBounds = true;
                Camera.SetBounds(room.RectPx);
            }
            else
            {
                ErEngine.LogWarning("shouldn't happen rn");
                Camera.UseBounds = false;
                Game.CurrentRoom = null;
            }
        }
        if(shouldSnap) Camera.SnapToPosition(point);
        else Camera.SetTargetPosition(point);
    }
    public static void SetPlayerPos(ErVec2 position)
    {
        PlayerPos = position;
    }
    public static SwMap GetMap()
    {
        return Map;
    }
    public SwGame()
    {
        if(!SwHud.TryLoad(ErVec2.Zero, out Hud))
        {
            ErEngine.LogError("bad hud");
            return;
        }
        Camera.DrawFn = DrawScene;
        Game = this;
        RenderTextures = new ErTexture[5];
        int idx;
        for (idx = 0; idx < RenderTextures.Length; idx++)
        {
            RenderTextures[idx] = ErTexture.GetRenderTexture((int)SwApp.CameraSize.X, (int)SwApp.CameraSize.Y);
        }
        // try load tile data
        string filepath = "game_data/map/tile_data.json";
        if(!SwApp.TryLoadPrion(filepath, out var tileData))
        {
            ErEngine.LogError("bad tile data");
            return;
        }
        TileData = new SwTileData[tileData.Count];
        idx = 0;
        ErVec2I tileSize = new(32,32);
        foreach (var item in tileData.Values)
        {
            if(!SwTileData.TryFromData(filepath, item, tileSize, out var data))
            {
                ErEngine.LogError("bad tile data at idx ", idx);
                return;
            }
            TileData[idx] = data;
            idx++;
        }
        AttachHandlers();
    }
    public void Update()
    {
        HandleFade();
        Map.Update();
        HandleRooms();
        CommandHandler.Dispatch();
        Camera.Update();
        // Note, all queued entities are added before ready is called
        foreach (var entity in NewEntities)
        {
            EntityLookup.TryAdd(entity.Id.ToString(), entity);
        }
        while(NewEntities.TryDequeue(out var entity))
        {
            entity.Ready();
        }
        foreach (var item in EntityLookup.GetAllValues<SwEntity>())
        {
            item.Update();
            if(item.IsFreeQueued) FreedEntities.Enqueue(item);
        }
        while(FreedEntities.TryDequeue(out var entity))
        {
            entity.GameCleanup();
            EntityLookup.Remove(entity.Id.ToString());
        }
        Map.PhysicsWorld.Update(DeltaTime);
        Hud.Update();
    }
    private void HandleFade()
    {
        if(FadeTarget == FadeState) return;
        double df = FadeDelta;
        FadeState += FadeDelta;
        bool finished = false;
        if(df > 0 && FadeState > FadeTarget) finished = true;
        else if(df < 0 && FadeState < FadeTarget) finished = true;
        if (finished)
        {
            FadeState = FadeTarget;
            SwApp.CommandStore.AddCommand(FadeOnFinishCommand);
            FadeOnFinishCommand = PriNull.Null;
        }
    }
    public void FadeIn()
    {
        FadeState = 1;
        FadeTarget = 0;
    }
    public void FadeOut()
    {
        FadeState = 0;
        FadeTarget = 1;
    }
    private static void CalculateFrameWeight()
    {
        if(DeltaTime > 0) FrameWeight += FrameDuration / DeltaTime;
        while(FrameWeight > 1) FrameWeight -= 1;
    }
    public void Draw()
    {
        CalculateFrameWeight();
        Camera.Draw();
        DrawFade();
        Hud.Draw();
    }
    private void SpawnEnt(PriNode command)
    {
        if(!command.TryGet("entity_type", out string entityType))
        {
            ErEngine.LogWarning("spawn entity command missing entity_type field");
            return;
        }
        PriDict props = new()
        {
            {"entity_type", command.Get("entity_type")},
            {"x", command.Get("x")},
            {"y", command.Get("y")},
            {"width_px", command.Get("width_px")},
            {"height_px", command.Get("height_px")},
            {"is_passive", command.Get("is_passive")},
        };
        var prototype = SwData.Prototypes.Get($"entities/{entityType}");
        props.Merge(prototype);
        switch (entityType)
        {
            case "none":
                break;
            case "slume":
                LoadEntity<SwSlume>(props);
                break;
            case "knight":
                LoadEntity<SwKnight>(props);
                break;
            case "aspect":
                LoadEntity<SwAspect>(props);
                break;
            default:
                ErEngine.LogWarning("tried to spawn unknown entity type '", entityType, "'");
                break;
        }
    }
    public void Launch(int numPlayers = 1)
    {
        FadeIn();
        for (int idx = 0; idx < numPlayers; idx++)
        {
            var player = LoadEntity<SwPlayer>(SwData.Prototypes.Get("entities/player"));
            Hud.Player = player;
            player.PlayerIdx = idx;
        }
        if(!Map.TryGetDefaultCheckpoint(out var checkpoint))
        {
            ErEngine.LogWarning("no default checkpoint found");
            return;
        }
        ActiveCheckpoint = checkpoint;
        SetCameraTarget(ActiveCheckpoint.RectPx.Center);
    }
    private void HandleFadeCommand(PriNode command)
    {
        if(!command.TryGet("verb", out string verb)) throw new("should be unreachable");
        FadeOnFinishCommand = command.Get("on_finish");
        if(verb == "game_fade_in") FadeIn();
        else if(verb == "game_fade_out") FadeOut();
        else throw new("should be unreachable");
    }
    private void AttachHandlers()
    {
        CommandHandler.AddHandler("game_spawn_entity", SpawnEnt);
        CommandHandler.AddHandler("game_fade_in", HandleFadeCommand);
        CommandHandler.AddHandler("game_fade_out", HandleFadeCommand);
    }
    private void HandleRooms()
    {
        if (CurrentRoom is not null && CurrentRoom.RectPx.Contains(PlayerPos)){}
        else if(!Map.TryGetRoom(PlayerPos, out var room))
        {
            CurrentRoom = null;
            Camera.UseBounds = false;
        }
        else
        {
            Camera.UseBounds = true;
            Camera.SetBounds(room.RectPx);
            CurrentRoom = room;
        }
    }
    private void DrawScene()
    {
        _RenderLayer = 0;
        ErEngine.Renderer.PushViewport(ErVec2.Zero, RenderTextures[RenderLayer]);
        for (int idx = 0; idx < RenderTextures.Length; idx++)
        {
            RenderLayer = idx;
            ErEngine.Renderer.Clear();
        }
        RenderLayer = 0;
        Map.Draw();
        if (SwApp.Debug)
        {
            Map.PhysicsWorld.DebugDrawTiles();
            Map.PhysicsWorld.DebugDrawBodies();
            Map.PhysicsWorld.DebugDrawAreas();
        }
        ErEngine.Renderer.FlushDebug();
        foreach (var entity in EntityLookup.GetAllValues<SwEntity>())
        {
            entity.Draw(entity);
        }
        ErEngine.Renderer.PopViewport();
        for (int idx = 0; idx < RenderTextures.Length; idx++)
        {
            RenderTextures[idx].Draw(ErVec2.Zero);
        }
    }
    private void DrawFade()
    {
        ErRect2 rect = new(0, SwApp.HUD_HEIGHT, SwApp.INTERNAL_WIDTH, SwApp.INTERNAL_HEIGHT);
        if(FadeState > 0) ErEngine.Renderer.DrawRect(rect, ErColor.Black, ErMath.Ease(FadeState, 1));
    }
    // public void AddEntity(SwEntity entity)
    // {
    //     NewEntities.Enqueue(entity);
    // }
    public T LoadEntity<T>(PriNode props) where T: SwEntity, new()
    {
        T ent = SwEntity.GameLoad<T>(props);
        NewEntities.Enqueue(ent);
        return ent;
    }
    public bool TryLoadMap(string filepath)
    {
        PriNode data;
        try
        {
            string text = File.ReadAllText(filepath);
            var json = JsonNode.Parse(text);
            data = PriParser.Parser.JsonToPrion(json);
        }
        catch
        {
            return false;
        }
        if(!SwMap.TryFromData(filepath, data, TileData, out var map)) return ErEngine.LogWarning("failed to load map '", filepath, "'.");
        _Map = map;
        map.LoadGlobals();
        map.DebugLoadAllRooms();
        PriDict command = [];
        command.TrySet("verb", "game_spawn_player");
        SwApp.CommandStore.AddCommand(command);
        return true;
    }
}