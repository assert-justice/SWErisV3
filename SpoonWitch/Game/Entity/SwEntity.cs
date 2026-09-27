using Eris;
using ErisMath;
using Prion.Db;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Rendering;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity;

public abstract class SwEntity
{
    private readonly Dictionary<(Type,string), SwComponent> ComponentLookup = [];
    private IEnumerable<SwComponent> Components => ComponentLookup.Values;
    public SwGame Game{get; private set;} = null!;
    public PriDb Props{get; private set;} = new(new PriDict());
    public virtual int RenderLayer => 1;
    public int Id{get; private set;}
    public ErVec2 Position;
    public bool Visible = true;
    public bool IsFreeQueued{get; private set;}
    private readonly Queue<PriNode> CommandQueue = [];
    private readonly Dictionary<string,Action<PriNode>> Handlers = [];
    private readonly Dictionary<string,Action<PriNode>> GlobalHandlers = [];
    protected void AddHandler(string verb, Action<PriNode> action)
    {
        if(!Handlers.TryAdd(verb, action)) ErEngine.LogWarning("tried to add duplicate handler: ", verb);
    }
    protected void AddGlobalHandler(string verb, Action<PriNode> action)
    {
        if(!GlobalHandlers.TryAdd(verb, action)) ErEngine.LogWarning("tried to add duplicate global: ", verb);
    }
    public void AddCommand(PriNode command)
    {
        CommandQueue.Enqueue(command);
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
    protected void HandleCommands()
    {
        while(CommandQueue.TryDequeue(out var command))
        {
            if(!command.TryGet("verb", out string verb))
            {
                ErEngine.LogWarning("bad command, no verb");
                return;
            }
            if(!Handlers.TryGetValue(verb, out var action)) continue;
            action(command);
        }
        foreach (var (verb, action) in GlobalHandlers)
        {
            foreach (var item in SwApp.CommandStore.GetCommands(verb))
            {
                action(item);
            }
        }
    }
    public void GameUpdate(double dt)
    {
        Update(dt);
        foreach (var comp in Components)
        {
            comp.Update(dt);
        }
        UpdateLate(dt);
    }
    public virtual void Update(double dt)
    {
        HandleCommands();
    }
    public virtual void UpdateLate(double dt)
    {
        foreach (var comp in Components)
        {
            comp.Update(dt);
        }
    }
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
