using System.Text.Json.Nodes;
using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using Prion.Parser;
using SpoonWitch.ByteStream;
using SpoonWitch.Command;
using SpoonWitch.Game.Entity;
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
    // public static readonly Dictionary<int, SwParticles2D> ParticleEmitters = [];
    // public static readonly Dictionary<int, SwInventory> InventoryLookup = [];
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
    // private static readonly SwEntPropsLookup PropsLookup = new();
    private SwMap? _Map = null;
    public static SwMap Map => Game._Map!;
    // private readonly Dictionary<byte, (SwEntity,SwEntity)> Prototypes = [];
    public readonly SwLookup EntityLookup = new();
    // private SwByteStream LastStream = new();
    // private SwByteStream NextStream = new();
    // private readonly SwByteStream NewEntities = new();
    private readonly Queue<SwEntity> NewEntities = [];
    private readonly Queue<SwEntity> FreedEntities = [];
    private SwRoom? CurrentRoom;
    private readonly SwHud Hud;
    public double FadeState = 0;
    // public double FadeState => ErMath.Ease(_FadeState, 5);
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
        // ParticleEmitters.Clear();
        // InventoryLookup.Clear();
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
    // public static bool TryGetEntProps(int id, out SwEntPropsBase entProps)
    // {
    //     return PropsLookup.TryGet(id, out entProps);
    // }
    // public static void PatchEnt(int head, ErVec2 position, ErVec2 velocity)
    // {
    //     Game.NextStream.SetHead(head);
    //     Game.NextStream.WriteVec2(position);
    //     Game.NextStream.WriteVec2(velocity);
    // }
    // private bool TryReadEnt(SwByteStream bs, out SwEntity primary)
    // {
    //     primary = default!;
    //     if(!bs.TryPeekByte(out byte typeId)) return false;
    //     if(!TryGetPrototype(typeId, out var pair)) return false;
    //     primary = pair.Item1;
    //     primary.Read(bs);
    //     return true;
    // }
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
        // (LastStream,NextStream) = (NextStream,LastStream);
        // LastStream.Reset();
        // NextStream.Clear();
        // while(TryReadEnt(LastStream, out var entity))
        // {
        //     entity.Update();
        //     if(!entity.IsFreeQueued) entity.Write(NextStream);
        //     else
        //     {
        //         PropsLookup.RemoveEntProps(entity);
        //         entity.GameCleanup();
        //     }
        // }
        // if(NewEntities.Head > 0)
        // {
        //     NewEntities.Reset();
        //     NextStream.Extend(NewEntities);
        //     NewEntities.Clear();
        // }
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
        switch (entityType)
        {
            case "none":
                break;
            case "slume":
                var slume = new SwSlume();
                slume.SetProps(command);
                AddEntity(slume);
                break;
            case "knight":
                SwKnight knight = new();
                knight.SetProps(command);
                AddEntity(knight);
                break;
            default:
                ErEngine.LogWarning("tried to spawn unknown entity type '", entityType, "'");
                break;
        }
    }
    public void Launch()
    {
        FadeIn();
        SwPlayer player = new();
        player.SetProps(new PriDict());
        Hud.Player = player;
        AddEntity(player);
        // player = new();
        // player.SetProps(new PriDict());
        // AddEntity(player);
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
    // private void RespawnPlayer(PriNode command)
    // {
    //     ErEngine.Log(command);
    // }
    private void AttachHandlers()
    {
        // CommandHandler.AddHandler("game_respawn_player", RespawnPlayer);
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
        // LastStream.Reset();
        // NextStream.Reset();
        // while(NextStream.BytesRemaining() > 0)
        // {
        //     NextStream.TryPeekByte(out byte typeId);
        //     if(!TryGetPrototype(typeId, out var pair)) continue;
        //     var (lastEnt, nextEnt) = pair;
        //     nextEnt.Read(NextStream);
        //     if(nextEnt.LastHeadIndex < 0) continue;
        //     LastStream.SetHead(nextEnt.LastHeadIndex);
        //     if(!LastStream.TryPeekByte(out _))
        //     {
        //         ErEngine.LogWarning("ent ", nextEnt.Id, " of type ", nextEnt.GetType(), " could not be read");
        //         continue;
        //     }
        //     lastEnt.Read(LastStream);
        //     lastEnt.Draw(nextEnt);
        // }
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
    public void AddEntity(SwEntity entity)
    {
        NewEntities.Enqueue(entity);
        // EntityLookup.TryAdd(entity.Id.ToString(), entity);
    }
    // private bool TryGetPrototype(byte typeId, out (SwEntity, SwEntity) pair)
    // {
    //     if(!Prototypes.TryGetValue(typeId, out pair)) return ErEngine.LogError("Unregistered type id '", typeId, "'.");
    //     return true;
    // }
    // private (T,T) GetPrototype<T>() where T: SwEntity, ISwEntity<T>
    // {
    //     if(!Prototypes.TryGetValue(T.TypeId, out var pair))
    //     {
    //         pair = (T.Primary,T.Secondary);
    //         Prototypes.Add(T.TypeId, pair);
    //     }
    //     var (p,s) = pair;
    //     if(p is not T primary) throw new("should be unreachable");
    //     if(s is not T secondary) throw new("should be unreachable");
    //     return (primary,secondary);
    // }
    // public void AddEntity<T>()where T: SwEntity, ISwEntity<T>
    // {
    //     AddEntityInternal<T>(new());
    // }
    // public void AddEntity<T>(PriNode entData) where T: SwEntity, ISwEntity<T>
    // {
    //     AddEntityInternal<T>(new(entData));
    // }
    // private void AddEntityInternal<T>(SwEntProps<T> entProps) where T: SwEntity, ISwEntity<T>
    // {
    //     GetPrototype<T>();
    //     PropsLookup.AddEntProps(entProps);
    //     entProps.Init(NewEntities);
    // }
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