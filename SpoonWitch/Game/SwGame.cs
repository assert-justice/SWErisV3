using System.Text.Json.Nodes;
using Eris;
using ErisMath;
using Prion.Node;
using Prion.Parser;
using SpoonWitch.ByteStream;
using SpoonWitch.Command;
using SpoonWitch.Game.Entity;
using SpoonWitch.Game.Entity.Actor.Enemy.Slume;
using SpoonWitch.Game.Entity.Actor.Player;
using SpoonWitch.Game.Map;
using SpoonWitch.UI.Hud;

namespace SpoonWitch.Game;

public class SwGame
{
    public static double DeltaTimeRaw{get => ErEngine.DeltaTime * GameSpeed;}
    public static double DeltaTime{get => Camera.IsInBounds() ? DeltaTimeRaw : 0;}
    public static double FrameTime{get => ErEngine.FrameDuration * AnimSpeed;}
    public static double FrameProgress{get => ErEngine.FrameTimeRemaining / DeltaTimeRaw;}
    public static double GameSpeed{get; private set;} = 1;
    public static double AnimSpeed{get; private set;} = 1;
    private static readonly Dictionary<int,SwEntProps> EntProps = [];
    private static SwMap Map = new();
    private static readonly Queue<SwMove> MoveQueue = [];
    private readonly Dictionary<byte, (SwEntity,SwEntity)> Prototypes = [];
    private SwByteStream LastStream = new();
    private SwByteStream NextStream = new();
    private readonly SwByteStream NewEntities = new();
    private SwRoom? CurrentRoom;
    private readonly SwHud Hud;
    public static readonly SwCamera Camera = new();
    public static ErVec2 PlayerPos{get; private set;} = new(32,32);
    private static readonly Queue<Action> QueuedActions = [];
    public static void SetPlayerPos(ErVec2 position)
    {
        PlayerPos = position;
        Camera.SetTargetPosition(position);
    }
    public static SwMap GetMap()
    {
        return Map;
    }
    private readonly struct SwMove
    {
        public int Id{get; init;}
        public uint Mask{get; init;}
        public ErVec2 Size{get; init;}
        public int Head{get; init;}
    }
    public SwGame()
    {
        if(!SwHud.TryLoad(ErVec2.Zero, out Hud)) throw new("bad hud");
        Camera.DrawFn = DrawScene;
        // AddEntity<SwPlayer>();
        // AddEntity(SwPlayer.Primary);
        // SwSlume.Primary.Position = new(256,256);
        // AddEntity(SwSlume.Primary);
    }
    public static void EnqueueAction(Action action)
    {
        QueuedActions.Enqueue(action);
    }
    public static void EnqueueCommandRect(uint mask, ErRect2 rect, SwCommand command)
    {
        void fn()
        {
            foreach (int id in Map.CollisionLayer.GetRectIds(rect, mask))
            {
                if(!TryGetEntProps(id, out var entProps)) continue;
                entProps.AddCommand(command);
            }
        }
        QueuedActions.Enqueue(fn);
    }
    public static bool TryGetEntProps(int id, out SwEntProps entProps)
    {
        return EntProps.TryGetValue(id, out entProps!);
    }
    public static void EnqueueMove(int id, uint mask, ErVec2 size, int head)
    {
        MoveQueue.Enqueue(new(){Id=id,Mask=mask,Size=size,Head=head});
    }
    public static void AddCollider(SwCollisionLayer.SwEntRect entRect)
    {
        Map.CollisionLayer.AddCollider(entRect);
    }
    public static IEnumerable<int> GetRectIds(ErRect2 rect, uint mask = uint.MaxValue)
    {
        return Map.CollisionLayer.GetRectIds(rect, mask);
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
        Map.CollisionLayer.ClearColliders();
        (LastStream,NextStream) = (NextStream,LastStream);
        LastStream.Reset();
        NextStream.Clear();
        while(TryReadEnt(LastStream, out var entity))
        {
            entity.Update();
            if(!entity.IsFreeQueued) entity.Write(NextStream);
            else EntProps.Remove(entity.Id);
        }
        if(NewEntities.Head > 0)
        {
            NewEntities.Reset();
            NextStream.Extend(NewEntities);
            NewEntities.Clear();
        }
        // Handle moves
        while(MoveQueue.TryDequeue(out var move))
        {
            NextStream.SetHead(move.Head);
            if(!NextStream.TryReadVec2(out var pos)) throw new("darn");
            if(!NextStream.TryReadVec2(out var vel)) throw new("poo");
            pos -= move.Size * 0.5;
            vel *= DeltaTime;
            Map.CollisionLayer.MoveAndSlide(move.Id, move.Mask, move.Size, ref pos, ref vel);
            pos += move.Size * 0.5;
            vel /= DeltaTime;
            NextStream.SetHead(move.Head);
            NextStream.WriteVec2(pos);
            NextStream.WriteVec2(vel);
        }
        while(QueuedActions.TryDequeue(out var action)) action();
        Hud.Update();
    }
    public void Draw()
    {
        Camera.Draw();
        Hud.Draw();
    }
    private void HandleCommands()
    {
        foreach (var command in SwApp.CommandStore.GetGlobalCommands("spawn_player"))
        {
            // Todo: implement this properly
            AddEntity<SwPlayer>(command.Payload);
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
        LastStream.Reset();
        NextStream.Reset();
        while(NextStream.BytesRemaining() > 0)
        {
            NextStream.TryPeekByte(out byte typeId);
            if(!TryGetPrototype(typeId, out var pair)) continue;
            // if(!Prototypes.TryGetValue(typeId, out var pair))
            // {
            //     ErEngine.LogError("Attempted to initalized entity with unregistered type id '", typeId, "'.");
            //     continue;
            // }
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
        AddEntity<T>(new SwEntProps());
    }
    public void AddEntity<T>(PriNode entData) where T: SwEntity, ISwEntity<T>
    {
        SwEntProps props = new(entData);
        AddEntity<T>(props);
    }
    public void AddEntity<T>(T entity, PriNode entData) where T: SwEntity, ISwEntity<T>
    {
        SwEntProps props = new(entData);
        AddEntity(entity, props);
    }
    public void AddEntity<T>(SwEntProps entProps) where T: SwEntity, ISwEntity<T>
    {
        var (primary,_) = GetPrototype<T>();
        AddEntity(primary, entProps);
    }
    public void AddEntity<T>(T entity, SwEntProps entProps) where T: SwEntity,ISwEntity<T>
    {
        GetPrototype<T>();
        entity.Init(entProps);
        EntProps.Add(entity.Id, entity.EntProps);
        entity.Write(NewEntities);
    }
    // private void AddEntityInternal<T>(T entity, SwEntProps entProps) where T: SwEntity,ISwEntity<T>
    // {
    //     GetPrototype<T>();
    //     // if(!Prototypes.TryGetValue(T.TypeId, out _))
    //     // {
    //     //     var pair = (T.Primary,T.Secondary);
    //     //     Prototypes.Add(T.TypeId, pair);
    //     // }
    //     entity.Init(entProps);
    //     EntProps.Add(entity.Id, entity.EntProps);
    //     entity.Write(NewEntities);
    // }
    public static bool TryLoadMap(string filepath)
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