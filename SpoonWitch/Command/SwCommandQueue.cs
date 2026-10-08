using Eris;
using Prion.Node;

namespace SpoonWitch.Command;

public class SwCommandQueue
{
    private readonly Queue<PriNode> CommandQueue = [];
    private readonly Queue<PriNode> Overflow = [];
    private readonly Dictionary<string, List<Action<PriNode>>> Handlers = [];
    private bool IsProcessing = false;
    public void AddHandler(string verb, Action<PriNode> handler)
    {
        if(!Handlers.TryGetValue(verb, out var actions))
        {
            actions = [];
            Handlers[verb] = actions;
        }
        actions.Add(handler);
    }
    public bool RemoveHandler(string verb, Action<PriNode> handler)
    {
        if(!Handlers.TryGetValue(verb, out var actions)) return false;
        if(!actions.Remove(handler)) return false;
        if(actions.Count == 0) Handlers.Remove(verb);
        return true;
    }
    public void AddCommand(PriNode command)
    {
        if(command is PriNull)
        {
            ErEngine.LogWarning("null command");
            return;
        }
        if(command.TryAs(out string str))
        {
            AddCommandVerb(str);
            return;
        }
        if(command is PriList list)
        {
            foreach (var item in list.Data)
            {
                AddCommand(item);
            }
            return;
        }
        if(!command.TryGet("verb", out string _))
        {
            ErEngine.LogWarning("malformed command, missing verb");
            return;
        }
        if(IsProcessing) Overflow.Enqueue(command);
        else CommandQueue.Enqueue(command);
    }
    public PriDict AddCommandVerb(string verb)
    {
        PriDict command = [];
        command.TrySet("verb", verb);
        AddCommand(command);
        return command;
    }
    public void Process()
    {
        IsProcessing = true;
        while(CommandQueue.TryDequeue(out var command))
        {
            if(!command.TryGet("verb", out string verb))
            {
                ErEngine.LogWarning("malformed command, missing verb, should be unreachable");
                return;
            }
            if(!Handlers.TryGetValue(verb, out var actions)) continue;
            foreach (var action in actions)
            {
                action(command);
            }
        }
        IsProcessing = false;
        while(Overflow.TryDequeue(out var command)) CommandQueue.Enqueue(command);
    }
    public void Clear()
    {
        CommandQueue.Clear();
        Overflow.Clear();
    }
}