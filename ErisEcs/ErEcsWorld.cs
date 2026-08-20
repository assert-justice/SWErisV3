namespace ErisEcs;

public class ErEcsWorld
{
    private readonly Dictionary<Type, int> PrototypeLookup = [];
    private readonly List<(ErEcsEntity,ErEcsEntity)> Prototypes = [];
    private ErByteStream LastStream;
    private ErByteStream NextStream;
    private int NextId = 0;
    private readonly ErByteStream NewEntities = new();
    public ErEcsWorld(int capacity = 0)
    {
        LastStream = new(capacity);
        NextStream = new(capacity);
    }
    public int GetNextId()
    {
        int id = NextId;
        NextId++;
        return id;
    }
    public int PeekNextId()
    {
        return NextId;
    }
    private bool TryReadEnt(ErByteStream byteStream, out ErEcsEntity entity)
    {
        entity = default!;
        return false;
    }
    public void Update()
    {
        //
        (LastStream,NextStream) = (NextStream,LastStream);
        LastStream.Reset();
        NextStream.Clear();
        while(TryReadEnt(LastStream, out var entity))
        {
            entity.Update();
            if(!entity.IsFreeQueued) entity.Write(NextStream);
            else entity.Cleanup();
        }
        if(NewEntities.Head > 0)
        {
            NewEntities.Reset();
            NextStream.Extend(NewEntities);
            NewEntities.Clear();
        }
    }
    public void Draw(){}
    public int AddPrototype<T>() where T: ErEcsEntity, new()
    {
        int typeId = Prototypes.Count;
        if(!PrototypeLookup.TryAdd(typeof(T), typeId)) throw new("attempted to add duplicate prototype, type '{typeof(T)}' is already registered");
        var primary = new T();
        primary.RegisterPrototype(this, typeId);
        var secondary = new T();
        secondary.RegisterPrototype(this, typeId);
        Prototypes.Add((primary, secondary));
        return typeId;
    }
    public bool TryGetPrototype<T>(out T prototype) where T: ErEcsEntity, new()
    {
        prototype = default!;
        if(!PrototypeLookup.TryGetValue(typeof(T), out int typeId)) return false;
        if(Prototypes[typeId].Item1 is not T pro) return false;
        prototype = pro;
        return true; 
    }
    public bool TryAddEntity<T>(T entity) where T: ErEcsEntity, new()
    {
        if(!PrototypeLookup.ContainsKey(typeof(T))) return false;
        entity.Init();
        entity.Write(NewEntities);
        return true;
    }
}
