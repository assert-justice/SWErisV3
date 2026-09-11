using Prion.Node;

namespace SpoonWitch.Command;

public class SwCommandHandler(SwCommandStore store)
{
    private readonly SwCommandStore Store = store;
    private readonly List<(Action<PriNode> handler, string verb)> GeneralHandlers = [];

    public void AddHandler(string verb, Action<PriNode> handler)
    {
        GeneralHandlers.Add((handler, verb));
    }
    public void AddHandler(string verb, Action handler)
    {
        void fn(PriNode priNode)
        {
            handler();
        }
        GeneralHandlers.Add((fn, verb));
    }
    public void Dispatch()
    {
        foreach (var (handler, verb) in GeneralHandlers)
        {
            foreach (var command in Store.GetGlobalCommands(verb))
            {
                handler(command);
            }
        }
    }
}