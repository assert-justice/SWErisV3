using ErisMath;
using SpoonWitch.ByteStream;
using SpoonWitch.Game.Map.Collision;

namespace SpoonWitch.Game.Entity.Component;

public class SwBodyComponent(SwEntity parent, string name, uint mask, ErVec2 size, ErVec2? offset = null, bool enabled = false) : SwComponent(parent, name)
{
    public SwColliderBody Body = new() { };
    private int _Id;
    public int Id => _Id;
    private bool WasEnabled = false;
    public bool Enabled = enabled;
    public uint Mask = mask;
    public int Head;
    public ErVec2 Offset = offset ?? ErVec2.Zero;
    public ErVec2 Position = ErVec2.Zero;
    public ErVec2 Size = size;
    public ErVec2 Velocity = ErVec2.Zero;
    public override void Ready()
    {
        base.Ready();
        _Id = SwApp.GetNextId();
    }
    public override void Update()
    {
        base.Update();
        if(Enabled != WasEnabled)
        {
            WasEnabled = Enabled;
            if (!Enabled) SwGame.GetMap().PhysicsWorld.RemoveBody(_Id);
        }
        if(!Enabled) return;
        Body.Position = Parent.Position + Offset;
        Body.Size = Size;
        Body.Mask = Mask;
        Body.ParentId = Parent.Id;
        SwGame.GetMap().PhysicsWorld.SetBody(_Id, Body);
        // int head = byteStream.Head;
        // // write type byte
        // byteStream.WriteByte(GetTypeId);
        // byteStream.WriteI32(_Id);
        // // write head position as current head index
        // byteStream.WriteI32(head);
        // // write current head index as last head index
        // // Note: if it is negative, that means there is no valid last head index. this is relevant for drawing.
        // byteStream.WriteI32(_CurrentHeadIndex);
        // Body.ParentId = Id;
        // Body.Position = Position;
        // Body.Velocity = Velocity;
        // Body.Mask = Mask;
        // Body.Head = byteStream.Head;
        // Body.Size = Size;
        // SwGame.Map.PhysicsWorld.SetBody(Id, Body);
        // byteStream.WriteVec2(Position);
        // byteStream.WriteVec2(Velocity);
        // byteStream.WriteBool(Visible);
    }
    public override void Read(SwByteStream byteStream)
    {
        base.Read(byteStream);
        if(!byteStream.TryReadI32(out _Id)) throw new("bad body id");
        if(!byteStream.TryReadBool(out WasEnabled)) throw new("bad body was enabled");
        if(!byteStream.TryReadBool(out Enabled)) throw new("bad body enabled");
        // if(!byteStream.TryReadU32(out Mask)) throw new("bad body mask"); // mask comes from the parent, not stored here
        if(!byteStream.TryReadVec2(out Offset)) throw new("bad body offset");
        // if(!byteStream.TryReadVec2(out Size)) throw new("bad body offset"); // size comes from the parent, not stored here
        if(!byteStream.TryReadI32(out Head)) throw new("bad body velocity");
        if(!byteStream.TryReadVec2(out Position)) throw new("bad body position");
        if(!byteStream.TryReadVec2(out Velocity)) throw new("bad body velocity");
        // set parent position to the body position minus the offset, to get the parents new position
        // note: order is important, the body should be the first component, and 
    }
    public override void Write(SwByteStream byteStream)
    {
        base.Write(byteStream);
        byteStream.WriteI32(_Id);
        byteStream.WriteBool(WasEnabled);
        byteStream.WriteBool(Enabled);
        // byteStream.WriteU32(Mask);
        byteStream.WriteVec2(Offset);
        // byteStream.WriteVec2(Size);
        byteStream.WriteI32(Head);
        byteStream.WriteVec2(Position);
        byteStream.WriteVec2(Velocity);
    }
}
