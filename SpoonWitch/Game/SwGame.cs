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
using SpoonWitch.Game.Map;
using SpoonWitch.Game.Map.Collision;
using SpoonWitch.UI.Hud;

namespace SpoonWitch.Game;

public class SwGame
{
    public static double DeltaTime => ErEngine.DeltaTime * GameSpeed;
    public static double FrameDuration => ErEngine.FrameDuration * GameSpeed;
    // The factor to blend between the last state and the next state with
    public static double FrameWeight{get; private set;}
    public static double GameSpeed => 1;
    private static readonly SwEntPropsLookup PropsLookup = new();
    public static SwMap Map{get; private set;} = new();
    // private static readonly Queue<SwMove> MoveQueue = [];
    private readonly Dictionary<byte, (SwEntity,SwEntity)> Prototypes = [];
    private SwByteStream LastStream = new();
    private SwByteStream NextStream = new();
    private readonly SwByteStream NewEntities = new();
    private readonly ErTexture HudBg = ErTexture.GetColoredTexture(SwApp.INTERNAL_WIDTH,SwApp.HUD_HEIGHT, new(131, 32, 185));
    private SwRoom? CurrentRoom;
    private readonly SwHud Hud;
    public static readonly SwCamera Camera = new();
    public static ErVec2 PlayerPos{get; private set;} = new(32,32);
    public static SwGame Game{get; private set;} = null!;
    public static void SetPlayerPos(ErVec2 position)
    {
        PlayerPos = position;
        Camera.SetTargetPosition(position);
    }
    public static SwMap GetMap()
    {
        return Map;
    }
    // private readonly struct SwMove
    // {
    //     public int Id{get; init;}
    //     public uint Mask{get; init;}
    //     public ErVec2 Size{get; init;}
    //     public int Head{get; init;}
    // }
    public SwGame()
    {
        if(!SwHud.TryLoad(ErVec2.Zero, out Hud))
        {
            ErEngine.LogError("bad hud");
            return;
        }
        Camera.DrawFn = DrawScene;
        Game = this;
    }
    public static bool TryGetEntProps(int id, out SwEntPropsBase entProps)
    {
        return PropsLookup.TryGet(id, out entProps);
    }
    public static void PatchEnt(int head, ErVec2 position, ErVec2 velocity)
    {
        Game.NextStream.SetHead(head);
        Game.NextStream.WriteVec2(position);
        Game.NextStream.WriteVec2(velocity);
    }
    private bool TryReadEnt(SwByteStream bs, out SwEntity primary)
    {
        primary = default!;
        if(!bs.TryPeekByte(out byte typeId)) return false;
        if(!TryGetPrototype(typeId, out var pair)) return false;
        primary = pair.Item1;
        primary.Read(bs);
        return true;
    }
    public void Update()
    {
        HandleRooms();
        HandleCommands();
        Camera.Update();
        (LastStream,NextStream) = (NextStream,LastStream);
        LastStream.Reset();
        NextStream.Clear();
        while(TryReadEnt(LastStream, out var entity))
        {
            entity.Update();
            if(!entity.IsFreeQueued) entity.Write(NextStream);
            else PropsLookup.RemoveEntProps(entity);
        }
        if(NewEntities.Head > 0)
        {
            NewEntities.Reset();
            NextStream.Extend(NewEntities);
            NewEntities.Clear();
        }
        Map.PhysicsWorld.Update(DeltaTime);
        Hud.Update();
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
        HudBg.Draw(ErVec2.Zero);
        Hud.Draw();
    }
    private void HandleCommands()
    {
        foreach (var command in SwApp.CommandStore.GetGlobalCommands("spawn_player"))
        {
            // Todo: implement this properly
            AddEntity<SwPlayer>(command);
        }
        foreach (var command in SwApp.CommandStore.GetGlobalCommands("spawn_entity"))
        {
            if(!command.TryGet("entity_type", out string entityType))
            {
                ErEngine.LogWarning("spawn entity command missing entity_type field");
                continue;
            }
            switch (entityType)
            {
                case "none":
                    break;
                case "slume":
                    AddEntity<SwSlume>(command);
                    break;
                case "knight":
                    AddEntity<SwKnight>(command);
                    break;
                default:
                    ErEngine.LogWarning("tried to spawn unknown entity type '", entityType, "'");
                    break;
            }
        }
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
            if(CurrentRoom is null) Camera.SnapToPosition(PlayerPos);
            CurrentRoom = room;
        }
    }
    private void DrawScene()
    {
        Map.Draw();
        if (SwApp.Debug)
        {
            // ErEngine.Log("here");
            Map.PhysicsWorld.DebugDrawTiles();
            Map.PhysicsWorld.DebugDrawBodies();
            Map.PhysicsWorld.DebugDrawAreas();
            ErEngine.Renderer.FlushDebug();
        }
        LastStream.Reset();
        NextStream.Reset();
        while(NextStream.BytesRemaining() > 0)
        {
            NextStream.TryPeekByte(out byte typeId);
            if(!TryGetPrototype(typeId, out var pair)) continue;
            var (lastEnt, nextEnt) = pair;
            nextEnt.Read(NextStream);
            if(nextEnt.LastHeadIndex < 0) continue;
            LastStream.SetHead(nextEnt.LastHeadIndex);
            if(!LastStream.TryPeekByte(out _))
            {
                ErEngine.LogWarning("ent ", nextEnt.Id, " of type ", nextEnt.GetType(), " could not be read");
                continue;
            }
            lastEnt.Read(LastStream);
            lastEnt.Draw(nextEnt);
        }
    }
    private bool TryGetPrototype(byte typeId, out (SwEntity, SwEntity) pair)
    {
        if(!Prototypes.TryGetValue(typeId, out pair)) return ErEngine.LogError("Unregistered type id '", typeId, "'.");
        return true;
    }
    private (T,T) GetPrototype<T>() where T: SwEntity, ISwEntity<T>
    {
        if(!Prototypes.TryGetValue(T.TypeId, out var pair))
        {
            pair = (T.Primary,T.Secondary);
            Prototypes.Add(T.TypeId, pair);
        }
        var (p,s) = pair;
        if(p is not T primary) throw new("should be unreachable");
        if(s is not T secondary) throw new("should be unreachable");
        return (primary,secondary);
    }
    public void AddEntity<T>()where T: SwEntity, ISwEntity<T>
    {
        AddEntityInternal<T>(new());
    }
    public void AddEntity<T>(PriNode entData) where T: SwEntity, ISwEntity<T>
    {
        AddEntityInternal<T>(new(entData));
    }
    private void AddEntityInternal<T>(SwEntProps<T> entProps) where T: SwEntity, ISwEntity<T>
    {
        GetPrototype<T>();
        PropsLookup.AddEntProps(entProps);
        entProps.Init(NewEntities);
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
        if(!SwMap.TryFromData(filepath, data, out var map)) return ErEngine.LogWarning("failed to load map '", filepath, "'.");
        Map = map;
        map.LoadGlobals();
        if(!map.TryGetDefaultCheckpoint(out var checkpoint)) return ErEngine.LogWarning("failed to find default checkpoint");
        checkpoint.Trigger();
        return true;
    }
}