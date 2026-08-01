using System.Text.Json.Nodes;
using Eris;
using ErisMath;
using Prion.Node;
using Prion.Parser;
using SpoonWitch.ByteStream;
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
        AddEntity(SwPlayer.Primary);
        SwSlume.Primary.Position = new(256,256);
        AddEntity(SwSlume.Primary);
    }
    public static void EnqueueAction(Action action)
    {
        QueuedActions.Enqueue(action);
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
    private bool TryReadEnt(SwByteStream bs, out SwEntity entity)
    {
        entity = default!;
        if(!bs.TryPeekByte(out byte typeId)) return false;
        if(!Prototypes.TryGetValue(typeId, out var pair))
        {
            return ErEngine.LogError("Unregistered type id '", typeId, "'.");
        }
        entity = pair.Item1;
        entity.Read(bs);
        return true;
    }
    public void Update()
    {
        HandleRooms();
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
        if(NewEntities.Head > 0)
        {
            NewEntities.Reset();
            while(TryReadEnt(NewEntities, out var entity))
            {
                entity.Write(NextStream);
            }
            NewEntities.Clear();
        }
        // if(QueuedActions.Count > 0) ErEngine.Log("q count: ", QueuedActions.Count);
        while(QueuedActions.TryDequeue(out var action)) action();
    }
    public void Draw()
    {
        Camera.Draw();
        Hud.Draw();
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
            if(!Prototypes.TryGetValue(typeId, out var pair))
            {
                ErEngine.LogError("Attempted to initalized entity with unregistered type id '", typeId, "'.");
                continue;
            }
            var (lastEnt, nextEnt) = pair;
            nextEnt.Read(NextStream);
            if(nextEnt.LastHeadIndex < 0) continue;
            LastStream.SetHead(nextEnt.LastHeadIndex);
            lastEnt.Read(LastStream);
            lastEnt.Draw(nextEnt);
        }
    }
    public void AddEntity<T>(T entity, SwEntProps? entProps = null) where T: SwEntity,ISwEntity<T>
    {
        if(!Prototypes.TryGetValue(T.TypeId, out _))
        {
            var pair = (T.Primary,T.Secondary);
            Prototypes.Add(T.TypeId, pair);
        }
        entity.Init(entProps ?? new());
        EntProps.Add(entity.Id, entity.EntProps);
        entity.Write(NewEntities);
    }
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
        return true;
    }
}