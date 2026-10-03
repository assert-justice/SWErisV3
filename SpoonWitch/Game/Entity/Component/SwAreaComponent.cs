using Eris;
using ErisMath;
using ErisPhysics2D.Collider;
using SpoonWitch.ByteStream;
using SpoonWitch.Game.Map.Collision;

namespace SpoonWitch.Game.Entity.Component;

public class SwAreaComponent: SwComponent
// (SwEntity parent, 
//     string name, uint mask, ErVec2 size, ErVec2? offset = null, bool enabled = false,
//     Action<SwEntity>? onBodyEnter = null, Action<SwEntity>? onBodyExit = null) : SwComponent(parent, name)
{
    private readonly SwColliderArea Area;
    private bool WasEnabled = false;
    public bool Enabled;
    public uint Mask;
    public ErVec2 Offset;
    public ErVec2 Size;

    public SwAreaComponent(SwEntity parent, string name, uint mask, ErVec2 size, ErVec2? offset = null, bool enabled = false,
        Action<SwEntity>? onBodyEnter = null, Action<SwEntity>? onBodyExit = null) : base(parent, name)
    {
        Enabled = enabled;
        Mask = mask;
        Offset = offset ?? ErVec2.Zero;
        Size = size;
        OnBodyEnter = onBodyEnter;
        OnBodyExit = onBodyExit;
        Area = new(SwApp.GetNextId(), parent.Id);
    }

    public Action<SwEntity>? OnBodyEnter{private get; set;}
    public Action<SwEntity>? OnBodyExit{private get; set;}
    public override void Ready()
    {
        base.Ready();
        Area.OnBodyEnterFn = OnEnter;
        Area.OnBodyExitFn = OnExit;
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        if(Enabled != WasEnabled)
        {
            WasEnabled = Enabled;
            if (!Enabled) Parent.Game.PhysicsWorld.RemoveArea(Area.Id);
        }
        if(!Enabled) return;
        Area.Position = Parent.Position + Offset - Size*0.5;
        Area.Size = Size;
        Area.Mask = Mask;
        Parent.Game.PhysicsWorld.AddArea(Area);
    }
    private void OnEnter(SwColliderArea area, ErColliderBody body)
    {
        if(OnBodyEnter is null) return;
        if(body is not SwColliderBody b) return;
        if(!Parent.Game.EntityLookup.TryGet<SwEntity>(b.ParentId.ToString(), out var entity)) return;
        OnBodyEnter(entity);
    }
    private void OnExit(SwColliderArea area, ErColliderBody body)
    {
        if(OnBodyExit is null) return;
        if(body is not SwColliderBody b) return;
        if(!Parent.Game.EntityLookup.TryGet<SwEntity>(b.ParentId.ToString(), out var entity)) return;
        OnBodyExit(entity);
    }
    // public override void Read(SwByteStream byteStream)
    // {
    //     base.Read(byteStream);
    //     if(!byteStream.TryReadI32(out _Id)) throw new("bad area id");
    //     if(!byteStream.TryReadBool(out WasEnabled)) throw new("bad area was enabled");
    //     if(!byteStream.TryReadBool(out Enabled)) throw new("bad area enabled");
    //     if(!byteStream.TryReadU32(out Mask)) throw new("bad area mask");
    //     if(!byteStream.TryReadVec2(out Offset)) throw new("bad area offset");
    //     if(!byteStream.TryReadVec2(out Size)) throw new("bad area offset");
    // }
    // public override void Write(SwByteStream byteStream)
    // {
    //     base.Write(byteStream);
    //     byteStream.WriteI32(_Id);
    //     byteStream.WriteBool(WasEnabled);
    //     byteStream.WriteBool(Enabled);
    //     byteStream.WriteU32(Mask);
    //     byteStream.WriteVec2(Offset);
    //     byteStream.WriteVec2(Size);
    // }
    public override void Cleanup()
    {
        base.Cleanup();
        Parent.Game.PhysicsWorld.RemoveArea(Area.Id);
    }
}