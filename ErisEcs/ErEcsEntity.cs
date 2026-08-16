namespace ErisEcs;

public abstract class ErEcsEntity: IErEcsObject
{
    public abstract int SizeBytes{get;}
    public bool IsFreeQueued{get; protected set;}
    private bool WasConstructorCalled = false;
    private readonly List<IErEcsObject> Components = [];
    public int TypeId{get; private set;} = -1;
    private readonly Dictionary<(string,Type), IErEcsObject> ComponentLookup = [];
    public void WorldSetTypeId(int typeId)
    {
        if(TypeId != -1) throw new("cannot set type id again");
        TypeId = typeId;
    }
    public void Init()
    {
        WasConstructorCalled = true;
        InitImpl();
    }
    protected virtual void InitImpl(){}
    public void Cleanup()
    {
        CleanupImpl();
    }
    protected virtual void CleanupImpl(){}
    protected void AddComponent(IErEcsObject component)
    {
        if(WasConstructorCalled) throw new("attempted to add a component after initialization");
        Components.Add(component);
    }
    protected void AddNamedComponent(IErEcsObject component, string name)
    {
        AddComponent(component);
        if(!ComponentLookup.TryAdd((name, component.GetType()), component)) throw new("attempted to add a duplicate named component");
    }
    public virtual void Update(){}
    public virtual void Draw(){}
    public bool TryRead(ErByteStream byteStream)
    {
        return TryReadImpl(byteStream);
    }
    protected virtual bool TryReadImpl(ErByteStream byteStream)
    {
        return true;
    }
    public void Write(ErByteStream byteStream)
    {
        if(!IsFreeQueued) return;
        WriteImpl(byteStream);
    }
    protected virtual void WriteImpl(ErByteStream byteStream){}
}