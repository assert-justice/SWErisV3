using Eris;
using ErisMath;
using SpoonWitch.ByteStream;
using SpoonWitch.Game.Map.Collision;

namespace SpoonWitch.Game.Entity.Component;

public class SwAreaComponent(SwEntity parent, string name, uint mask, ErVec2 size, ErVec2? offset = null, bool enabled = false) : SwComponent(parent, name)
{
    public SwColliderArea Area = new() { };
    private int _Id;
    public int Id => _Id;
    private bool WasEnabled = false;
    public bool Enabled = enabled;
    public uint Mask = mask;
    public ErVec2 Offset = offset ?? ErVec2.Zero;
    public ErVec2 Size = size;
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
            if (!Enabled) SwGame.GetMap().PhysicsWorld.RemoveArea(_Id);
        }
        if(!Enabled) return;
        Area.Position = Parent.Position + Offset;
        Area.Size = Size;
        Area.Mask = Mask;
        SwGame.GetMap().PhysicsWorld.SetArea(_Id, Area);
    }
    public override void Read(SwByteStream byteStream)
    {
        base.Read(byteStream);
        if(!byteStream.TryReadI32(out _Id)) throw new("bad area id");
        if(!byteStream.TryReadBool(out WasEnabled)) throw new("bad area was enabled");
        if(!byteStream.TryReadBool(out Enabled)) throw new("bad area enabled");
        if(!byteStream.TryReadU32(out Mask)) throw new("bad area mask");
        if(!byteStream.TryReadVec2(out Offset)) throw new("bad area offset");
        if(!byteStream.TryReadVec2(out Size)) throw new("bad area offset");
    }
    public override void Write(SwByteStream byteStream)
    {
        base.Write(byteStream);
        byteStream.WriteI32(_Id);
        byteStream.WriteBool(WasEnabled);
        byteStream.WriteBool(Enabled);
        byteStream.WriteU32(Mask);
        byteStream.WriteVec2(Offset);
        byteStream.WriteVec2(Size);
    }
}