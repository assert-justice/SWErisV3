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
    public PriDb Props{get; private set;} = new(new PriDict());
    public virtual int RenderLayer => 1;
    public int Id{get; private set;}
    public ErVec2 Position;
    public bool Visible = true;
    public bool IsFreeQueued{get; private set;}
    protected virtual int NumClocks => 0;
    protected readonly double[] Clocks;
    private readonly Queue<PriNode> CommandQueue = [];
    private readonly Dictionary<string,Action<PriNode>> Handlers = [];
    private readonly Dictionary<string,Action<PriNode>> GlobalHandlers = [];
    public SwEntity()
    {
        Clocks = new double[NumClocks];
        Array.Fill(Clocks, 0);
    }
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
    protected virtual void SetProps(PriNode props)
    {
        Props = new(props);
        Position = SwPrion.GetVec2(Props.Data);
    }
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
    public virtual void Update()
    {
        HandleCommands();
        foreach (var comp in Components)
        {
            comp.Update();
        }
    }
    public void Draw(SwEntity nextState)
    {
        if(!Visible) return;
        SwGame.RenderLayer = RenderLayer;
        DrawImpl(nextState);
        foreach (var item in Components)
        {
            item.Draw(item);
        }
        DrawImplLate(nextState);
    }
    protected virtual void DrawImpl(SwEntity nextState){}
    protected virtual void DrawImplLate(SwEntity nextState){}
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
        // ErEngine.LogWarning()
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
            if(!SwSprite.TryFromData(out var sprite, item)) continue;
            RegisterComponent(new SwSpriteComponent(this, sprite));
        }
    }
    // protected bool TryLoadSprites(string filepath)
    // {
    //     if(!SwApp.TryLoadPrion(filepath, out var priNode)) return false;
    //     string dirpath = Path.GetDirectoryName(filepath)!;
    //     if(!priNode.TryGet("sprites", out PriDict dict)) return false;
    //     foreach (var (name, node) in dict.Data)
    //     {
    //         if(!SwSprite.TryFromData(out var sprite, name, dirpath, node)) ErEngine.LogWarning("failed to parse sprite '", name, "'");
    //         else RegisterComponent(new SwSpriteComponent(this, sprite));
    //     }
    //     return true;
    // }
    public virtual void GameCleanup()
    {
        // Note: this method should only be called by game
        foreach (var item in Components)
        {
            item.Cleanup();
        }
    }
    public static T GameLoad<T>(PriNode props) where T: SwEntity, new()
    {
        T ent = new();
        if(props.TryGet("id", out int id)) ent.Id = id;
        else ent.Id = SwApp.GetNextId();
        ent.SetProps(props);
        return ent;
    }
}
