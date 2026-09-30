using Eris;
using ErisMath;
using Prion.Db;
using Prion.Node;
using SpoonWitch.Command;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Rendering;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity;

public abstract class SwEntity
{
    private readonly Dictionary<(Type,string), SwComponent> ComponentLookup = [];
    private readonly List<(string verb, Action<PriNode> handler)> GlobalHandlers = [];
    private IEnumerable<SwComponent> Components => ComponentLookup.Values;
    public SwGame Game{get; private set;} = null!;
    public PriDb Props{get; private set;} = new(new PriDict());
    public virtual int RenderLayer => 1;
    public int Id{get; private set;}
    public ErVec2 Position;
    public bool Visible = true;
    public bool IsFreeQueued{get; private set;}
    private readonly SwCommandQueue CommandQueue = new();
    protected readonly SwClockGroup Clocks = new();
    protected void AddHandler(string verb, Action<PriNode> action)
    {
        CommandQueue.AddHandler(verb, action);
    }
    protected void AddGlobalHandler(string verb, Action<PriNode> action)
    {
        SwApp.CommandQueue.AddHandler(verb, action);
        GlobalHandlers.Add((verb,action));
    }
    public void AddCommand(PriNode command)
    {
        CommandQueue.AddCommand(command);
    }
    protected SwComponent RegisterComponent(SwComponent component)
    {
        if(!ComponentLookup.TryAdd((component.GetType(), component.Name), component)) ErEngine.LogError("Failed to register component of name '", component.Name, "' and type '", component.GetType(), "'.");
        return component;
    }
    public virtual void SetProps(PriNode props)
    {
        Props = new(props);
        Position = SwPrion.GetVec2(Props.Data);
    }
    // Note: Init is called after the initial props have been set but before ready is called
    public virtual void Init(){}
    public virtual void Ready()
    {
        foreach (var item in Components)
        {
            item.Ready();
        }
    }
    public void QueueFree()
    {
        IsFreeQueued = true;
    }
    public void GameUpdate(double dt)
    {
        CommandQueue.Process();
        Update(dt);
        foreach (var comp in Components)
        {
            comp.Update(dt);
        }
        UpdateLate(dt);
    }
    protected virtual void Update(double dt)
    {
    }
    protected virtual void UpdateLate(double dt){}
    public void GameDraw()
    {
        if(!Visible) return;
        Game.SetRenderLayer(RenderLayer);
        Draw();
        foreach (var item in Components)
        {
            item.Draw();
        }
        DrawLate();
    }
    protected virtual void Draw(){}
    protected virtual void DrawLate(){}
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
        foreach (var item in ComponentLookup.Values)
        {
            ErEngine.Log(item.Name);
        }
        return null;
    }
    protected void LoadSprites(string propsPath)
    {
        foreach (var item in Props.Get(propsPath).Values)
        {
            if(!SwSprite.TryFromData(out var sprite, item)) ErEngine.LogWarning("failed to load sprite");
            else RegisterComponent(new SwSpriteComponent(this, sprite));
        }
    }
    public virtual void GameCleanup()
    {
        // Note: this method should only be called by game
        foreach (var item in Components)
        {
            item.Cleanup();
        }
        foreach (var (verb,action) in GlobalHandlers)
        {
            SwApp.CommandQueue.RemoveHandler(verb, action);
        }
    }
    public static T GameLoad<T>(SwGame game, PriNode props) where T: SwEntity, new()
    {
        T ent = new()
        {
            Game = game
        };
        if (props.TryGet("id", out int id)) ent.Id = id;
        else ent.Id = SwApp.GetNextId();
        ent.SetProps(props);
        ent.Init();
        return ent;
    }
}
