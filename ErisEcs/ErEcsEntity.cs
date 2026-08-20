using ErisEcs.EcsComponent;

namespace ErisEcs;

public abstract class ErEcsEntity
{
    // Singleton fields
    public int TypeId{get; private set;} = -1;
    public bool IsRegistered => TypeId >= 0;
    private ErEcsWorld World = null!;
    private readonly List<ErEcsBaseComponent> AllComponents = [];
    private readonly List<ErEcsBaseComponent> DrawableComponents = [];
    private readonly Dictionary<(string,Type), ErEcsBaseComponent> NamedComponents = [];
    // This field is special. It is an instance field but it isn't read from or written to the byte stream. 
    // Instead it is set to true at the beginning of each update, and if it is false at the end of the update the entity is freed.
    public bool IsFreeQueued{get; protected set;}
    // Instance fields, to be read/written
    private int _Id;
    public int Id => _Id;
    private int CurrentHeadIndex = -1;
    private int _LastHeadIndex = -1;
    public int LastHeadIndex => _LastHeadIndex;
    public void RegisterPrototype(ErEcsWorld world, int typeId)
    {
        if(TypeId != -1) throw new("cannot set type id again");
        TypeId = typeId;
        World = world;
        CurrentHeadIndex = -1;
        _LastHeadIndex = -1;
        foreach (var comp in AllComponents)
        {
            comp.RegisterComponent();
        }
    }
    public void Init()
    {
        _Id = World.GetNextId();
        Ready();
        foreach (var comp in AllComponents)
        {
            comp.Ready();
        }
    }
    protected virtual void Ready(){}
    public void Cleanup()
    {
        CleanupImpl();
        IsFreeQueued = true;
    }
    protected virtual void CleanupImpl(){}
    protected void AddComponent<T>(T component, string? name = null) where T: ErEcsBaseComponent
    {
        if(IsRegistered) throw new("attempted to add a component after entity was registered");
        if(name is not null)
        {
            if(!NamedComponents.TryAdd((name, component.GetType()), component)) throw new($"a component of type {component.GetType()} named {name} has already been registered");
        }
        AllComponents.Add(component);
        if(component.IsDrawable) DrawableComponents.Add(component);
    }
    public void Update()
    {
        UpdateImpl();
        foreach (var comp in AllComponents)
        {
            comp.Update();
        }
    }
    protected virtual void UpdateImpl(){}
    public void Draw(ErEcsEntity nextTickEntity)
    {
        DrawImpl(nextTickEntity);
    }
    protected virtual void DrawImpl(ErEcsEntity nextTickEntity){}
    public bool TryRead(ErByteStream byteStream)
    {
        // type id is read by world, does not need to be read again
        // ent id
        if(!byteStream.TryReadI32(out _Id)) return false;
        // current head
        if(!byteStream.TryReadI32(out CurrentHeadIndex)) return false;
        // last head
        if(!byteStream.TryReadI32(out _LastHeadIndex)) return false;
        return TryReadImpl(byteStream);
    }
    protected virtual bool TryReadImpl(ErByteStream byteStream)
    {
        return true;
    }
    public void Write(ErByteStream byteStream)
    {
        if(!IsFreeQueued) return;
        int head = byteStream.Head;
        // type id
        byteStream.WriteI32(TypeId);
        // ent id
        byteStream.WriteI32(_Id);
        // current head
        byteStream.WriteI32(head);
        // last head
        byteStream.WriteI32(CurrentHeadIndex);
        WriteImpl(byteStream);
    }
    protected virtual void WriteImpl(ErByteStream byteStream){}
}