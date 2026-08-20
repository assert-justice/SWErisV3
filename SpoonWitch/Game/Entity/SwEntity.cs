using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.ByteStream;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Map.Collision;
using SpoonWitch.Rendering;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity;

public abstract class SwEntity
{
    private readonly Dictionary<(Type,string), SwComponent> ComponentLookup = [];
    private readonly List<SwComponent> Components = [];
    public SwEntPropsBase EntProps{get; private set;} = null!;
    public virtual int RenderLayer => 1;
    abstract protected byte GetTypeId{get;}
    private int _Id;
    public int Id => _Id;
    private int _CurrentHeadIndex = -1;
    public int CurrentHeadIndex{get => _CurrentHeadIndex;}
    private int _LastHeadIndex = -1;
    public int LastHeadIndex{get => _LastHeadIndex;}
    public ErVec2 Position;
    public ErVec2 Velocity;
    public bool Visible = true;
    public virtual ErVec2 Size => new(32,32);
    public virtual uint Mask => 0;
    public bool IsFreeQueued{get; private set;}
    protected virtual int NumClocks => 0;
    protected readonly double[] Clocks;
    // private bool WasBodyEnabled = false;
    // public bool BodyEnabled = true;
    private readonly SwColliderBody Body = new();
    public SwEntity()
    {
        Clocks = new double[NumClocks];
    }
    protected SwComponent RegisterComponent(SwComponent component)
    {
        // Note: this method should only really be used from the entity's constructor
        // Todo: make improper use throw an exception? or log a warning?
        if(ComponentLookup.TryAdd((component.GetType(), component.Name), component)) Components.Add(component);
        else ErEngine.LogError("Failed to register component of name '", component.Name, "' and type '", component.GetType(), "'.");
        return component;
    }
    public void Init(SwEntPropsBase entProps)
    {
        _Id = entProps.Id;
        EntProps = entProps;
        _CurrentHeadIndex = -1;
        _LastHeadIndex = -1;
        Array.Fill(Clocks, 0);
        Ready();
    }
    public virtual void Ready()
    {
        Position = SwPrion.GetVec2(EntProps.Props.Data);
        foreach (var item in Components)
        {
            item.Ready();
        }
    }
    public virtual void Read(SwByteStream byteStream)
    {
        // read type byte
        if(!byteStream.TryReadByte(out _)) throw new("no type id");
        if(!byteStream.TryReadI32(out _Id)) throw new("jerkbag");
        if(!byteStream.TryReadI32(out _CurrentHeadIndex)) throw new("oops2");
        if(!byteStream.TryReadI32(out _LastHeadIndex)) throw new("oops3");
        if(!byteStream.TryReadVec2(out Position)) throw new("oops4");
        if(!byteStream.TryReadVec2(out Velocity)) throw new("oops5");
        if(!byteStream.TryReadBool(out Visible)) throw new("oops6");
        // if(!byteStream.TryReadBool(out WasBodyEnabled)) throw new("oops7");
        // if(!byteStream.TryReadBool(out BodyEnabled)) throw new("oops7");
        // Position = BodyEnabled ? pos + Size * 0.5 : pos;
        // read clocks
        if(!byteStream.TryReadF64s(in Clocks)) throw new("bad clocks");
        // read components
        foreach (var item in Components)
        {
            item.Read(byteStream);
        }
        if(!SwGame.TryGetEntProps(Id, out var entProps)) ErEngine.LogError("no properties found for for entity ", Id);
        EntProps = entProps!;
    }
    protected void QueueFree()
    {
        IsFreeQueued = true;
    }
    protected virtual void HandleCommands(){}
    public virtual void Write(SwByteStream byteStream)
    {
        if(IsFreeQueued) return; // Todo: prevent child classes from writing as well
        int head = byteStream.Head;
        // write type byte
        byteStream.WriteByte(GetTypeId);
        byteStream.WriteI32(_Id);
        // write head position as current head index
        byteStream.WriteI32(head);
        // write current head index as last head index
        // Note: if it is negative, that means there is no valid last head index. this is relevant for drawing.
        byteStream.WriteI32(_CurrentHeadIndex);
        // if(BodyEnabled != WasBodyEnabled)
        // {
        //     WasBodyEnabled = BodyEnabled;
        //     if(!BodyEnabled) SwGame.Map.PhysicsWorld.RemoveBody(Id);
        // }
        // if (BodyEnabled)
        // {
        Body.ParentId = Id;
        Body.Rect = ErRect2.Centered(Position, Size);
        // Body.Position = Position - Size * 0.5;
        // Body.Size = Size;
        Body.Velocity = Velocity;
        Body.Mask = Mask;
        Body.Head = byteStream.Head;
        SwGame.Map.PhysicsWorld.SetBody(Id, Body);
        // }
        byteStream.WriteVec2(Position);
        byteStream.WriteVec2(Velocity);
        byteStream.WriteBool(Visible);
        // byteStream.WriteBool(WasBodyEnabled);
        // byteStream.WriteBool(BodyEnabled);
        // clocks
        byteStream.WriteF64s(in Clocks);
        // write components
        foreach (var item in Components)
        {
            item.Write(byteStream);
        }
    }
    public virtual void Update()
    {
        // CommandHandler.Dispatch();
        IsFreeQueued = false;
        HandleCommands();
        foreach (var comp in Components)
        {
            comp.Update();
        }
    }
    public void Draw(SwEntity nextState)
    {
        if(!Visible) return;
        if(nextState.GetType() != GetType()) throw new Exception("type mismatch");
        if(nextState.Components.Count != Components.Count) throw new Exception("component mismatch");
        SwGame.RenderLayer = RenderLayer;
        DrawImpl(nextState);
        for (int idx = 0; idx < Components.Count; idx++)
        {
            var comp = Components[idx];
            var nextComp = nextState.Components[idx];
            comp.Draw(nextComp);
        }
    }
    protected virtual void DrawImpl(SwEntity nextState){}
    public bool TryGetComponent<T>(string name, out T component) where T: SwComponent
    {
        component = null!;
        if(!ComponentLookup.TryGetValue((typeof(T),name), out var comp)) return false;
        if(comp is not T c) return false;
        component = c;
        return true;
    }
    public T? GetComponent<T>(string name) where T: SwComponent
    {
        if(TryGetComponent(name, out T component)) return component;
        ErEngine.LogWarning("entity does not have a valid '", name, "' component");
        return null;
    }
    protected bool TryLoadSprites(string filepath)
    {
        if(!SwApp.TryLoadPrion(filepath, out var priNode)) return false;
        string dirpath = Path.GetDirectoryName(filepath)!;
        if(!priNode.TryGet("sprites", out PriDict dict)) return false;
        foreach (var (name, node) in dict.Data)
        {
            if(!SwSprite.TryFromData(out var sprite, name, dirpath, node)) ErEngine.LogWarning("failed to parse sprite '", name, "'");
            else RegisterComponent(new SwSpriteComponent(this, sprite));
        }
        return true;
    }
}