namespace SpoonWitch.Command;

public class SwCommandHandler(SwCommandStore store)
{
    private readonly SwCommandStore Store = store;
    private readonly List<(Action<SwCommand> handler, string verb)> GeneralHandlers = [];
    private readonly List<(Action<SwCommand> handler, string verb, int targetId)> TargetedHandlers = [];

    public void AddHandler(Action<SwCommand> handler, string verb, int targetId = -1)
    {
        if(targetId < 0) GeneralHandlers.Add((handler, verb));
        else TargetedHandlers.Add((handler, verb, targetId));
    }
    public void Dispatch()
    {
        foreach (var (handler, verb) in GeneralHandlers)
        {
            foreach (var command in Store.GetCommands(verb))
            {
                handler(command);
            }
        }
        foreach (var (handler, verb, targetId) in TargetedHandlers)
        {
            foreach (var command in Store.GetCommands(verb, targetId))
            {
                handler(command);
            }
        }
    }
}