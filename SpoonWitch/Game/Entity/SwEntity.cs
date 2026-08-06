using System.Text.Json.Nodes;
using Eris;
using ErisMath;
using Prion.Node;
using Prion.Parser;
using SpoonWitch.ByteStream;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.Sprite;

namespace SpoonWitch.Game.Entity;

public abstract class SwEntity
{
    private readonly Dictionary<(Type,string), SwComponent> ComponentLookup = [];
    private readonly List<SwComponent> Components = [];
    public SwEntPropsBase EntProps{get; private set;} = null!;
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
    public virtual uint Mask => 1;
    public bool IsFreeQueued{get; private set;}
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
        Ready();
    }
    public virtual void Ready()
    {
        if(!EntProps.Props.TryGet("x_px", out double x)) x = 0;
        if(!EntProps.Props.TryGet("y_px", out double y)) y = 0;
        Position = new(x,y);
    }
    public virtual void Read(SwByteStream byteStream)
    {
        // read type byte
        if(!byteStream.TryReadByte(out _)) throw new Exception("no type id");
        if(!byteStream.TryReadI32(out _Id)) throw new Exception("jerkbag");
        if(!byteStream.TryReadI32(out _CurrentHeadIndex)) throw new Exception("oops2");
        if(!byteStream.TryReadI32(out _LastHeadIndex)) throw new Exception("oops3");
        if(!byteStream.TryReadVec2(out Position)) throw new Exception("oops4");
        if(!byteStream.TryReadVec2(out Velocity)) throw new Exception("oops5");
        if(!byteStream.TryReadBool(out Visible)) throw new Exception("oops6");
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
        if(IsFreeQueued) return;
        int head = byteStream.Head;
        // write type byte
        byteStream.WriteByte(GetTypeId);
        byteStream.WriteI32(_Id);
        // write head position as current head index
        byteStream.WriteI32(head);
        // write current head index as last head index
        // Note: if it is negative, that means there is no valid last head index. this is relevant for drawing.
        byteStream.WriteI32(_CurrentHeadIndex);
        if (Velocity.IsNonzero())
        {
            // queue move
            SwGame.EnqueueMove(Id,Mask,Size,byteStream.Head);
        }
        byteStream.WriteVec2(Position);
        byteStream.WriteVec2(Velocity);
        byteStream.WriteBool(Visible);
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
        for (int idx = 0; idx < Components.Count; idx++)
        {
            var comp = Components[idx];
            var nextComp = nextState.Components[idx];
            comp.Draw(nextComp);
        }
    }
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
        // Todo: cache filepaths?
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
        if(!data.Get("sprites").TryAs(out PriDict sprites))
        {
            return ErEngine.LogError("not a dictionary");
        }
        foreach (var (name, spriteData) in sprites.Data)
        {
            if(!spriteData.TryAs(out PriDict spriteDict))
            {
                return ErEngine.LogError("bad sprite data");
            }
            if(!SwSprite.TryFromData(filepath, spriteDict, this, name, out var sprite))
            {
                return ErEngine.LogError("failed to create sprite");
            }
            RegisterComponent(sprite);
        }
        return true;
    }
}