using ErisMath;

namespace ErisPhysics2D.Collider;

public abstract class ErCollider
{
    public int ParentId;
    public ErVec2 Position
    {
        get => _Position.Value;
        set => _Position.Value = value;
    }
    public ErVec2 Size
    {
        get => _Size.Value;
        set => _Size.Value = value;
    }
    public uint Mask
    {
        get => _Mask.Value;
        set => _Mask.Value = value;
    }
    public ErRect2 Rect => new(Position,Size);
    private readonly ErDirtyFlag<ErVec2> _Position;
    private readonly ErDirtyFlag<ErVec2> _Size;
    private readonly ErDirtyFlag<uint> _Mask;
    internal virtual bool IsDirty => _Position.IsDirty || _Size.IsDirty || _Mask.IsDirty;
    public ErCollider(ErVec2? position = null, ErVec2? size = null, uint mask = 0)
    {
        _Position = new(position ?? ErVec2.Zero);
        _Size = new(size ?? ErVec2.Zero);
        _Mask = new(mask);
    }
    // public virtual bool TryCopy<T>(ref T value) where T: ErCollider
    // {
    //     value.Position = Position;
    //     value.Size = Size;
    //     value.Mask = Mask;
    //     value.ParentId = ParentId;
    //     return true;
    // }
    public virtual void Copy<T>(ref T value) where T: ErCollider
    {
        value.Position = Position;
        value.Size = Size;
        value.Mask = Mask;
        value.ParentId = ParentId;
    }
    internal void Clean()
    {
        _Position.Clean();
        _Size.Clean();
        _Mask.Clean();
    }
    public virtual void OnRemove(){}
}
