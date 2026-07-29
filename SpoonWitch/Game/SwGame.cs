using System.Text.Json.Nodes;
using Eris;
using ErisMath;
using Prion.Node;
using Prion.Parser;
using SpoonWitch.ByteStream;
using SpoonWitch.Game.Entity;
using SpoonWitch.Game.Entity.Actor.Player;
using SpoonWitch.Game.Map;
using SpoonWitch.UI.Hud;

namespace SpoonWitch.Game;

public class SwGame
{
    public static double DeltaTimeRaw{get => ErEngine.DeltaTime * GameSpeed;}
    public static double DeltaTime{get => Camera.IsInBounds() ? DeltaTimeRaw : 0;}
    public static double FrameTime{get => ErEngine.FrameTime * AnimSpeed;}
    public static double FrameProgress{get => ErEngine.FrameTimeRemaining / DeltaTimeRaw;}
    public static double GameSpeed{get; private set;} = 1;
    public static double AnimSpeed{get; private set;} = 1;
    private static SwMap Map = new();
    private static readonly Queue<SwMove> MoveQueue = [];
    private readonly Dictionary<byte, (SwEntity,SwEntity)> Prototypes = [];
    private readonly Dictionary<Type, (SwEntity,SwEntity)> TypeLookup = [];
    private readonly Queue<SwEntity> NewEntities = [];
    private SwByteStream LastStream = new();
    private SwByteStream NextStream = new();
    private SwRoom? CurrentRoom;
    private readonly SwHud Hud = new();
    private static readonly SwCamera Camera = new();
    private static ErVec2 PlayerPos = new(32,32);
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
        // Camera = new(DrawScene);
        Camera.DrawFn = DrawScene;
    }
    // public void Init()
    // {
    //     AddEntity(new SwPlayer());
    // }
    public static void EnqueueMove(int id, uint mask, ErVec2 size, int head)
    {
        MoveQueue.Enqueue(new(){Id=id,Mask=mask,Size=size,Head=head});
    }
    // public static void MoveAndSlide(int id, uint mask, ErVec2 size, ref ErVec2 position, ref ErVec2 velocity)
    // {
    //     ErVec2 vel = velocity * DeltaTime;
    //     Map.CollisionLayer.MoveAndSlide(id, mask, size, ref position, ref vel);
    //     velocity = vel / DeltaTime;
    // }
    public static void AddCollider(SwCollisionLayer.SwEntRect entRect)
    {
        Map.CollisionLayer.AddCollider(entRect);
    }
    public void Update()
    {
        HandleRooms();
        Camera.Update();
        // SwCollisionLayer.SwEntRect temp = new(){Id = 1, Mask = 1, Rect = new(256,512-128+32,128,128)};
        Map.CollisionLayer.ClearColliders();
        // Map.CollisionLayer.AddCollider(temp);
        (LastStream,NextStream) = (NextStream,LastStream);
        LastStream.Reset();
        NextStream.Clear();
        while(LastStream.BytesRemaining() > 0)
        {
            LastStream.TryPeekByte(out byte typeId);
            if(!Prototypes.TryGetValue(typeId, out var pair))
            {
                ErEngine.LogError("Unregistered type id '", typeId, "'.");
                continue;
            }
            var (ent,_) = pair;
            ent.Read(LastStream);
            ent.Update();
            ent.Write(NextStream);
        }
        while(NewEntities.TryDequeue(out var entity))
        // foreach (var entity in NewEntities)
        {
            // init entity
            if(!TypeLookup.TryGetValue(entity.GetType(), out var pair))
            {
                pair = (entity, entity.New());
                TypeLookup.Add(entity.GetType(), pair);
                Prototypes.Add(0, pair);
            }
            pair.Item1.Write(NextStream);
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
    }
    public void Draw()
    {
        Camera.Draw();
        Hud.Draw();
        // DrawScene();
    }
    private void HandleRooms()
    {
        // if(CurrentRoom is null) return;// Camera.UseBounds = false;
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
        Map.DebugDraw();
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
    public void AddEntity(SwEntity entity)
    {
        NewEntities.Enqueue(entity);
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
        // Camera.SnapToPosition()
        // if(map.TryGetRoom(new(32,32), out var room))
        // {
        //     Camera.UseBounds = true;
        //     Camera.SetBounds(room.RectPx);
        // }
        return true;
    }
}