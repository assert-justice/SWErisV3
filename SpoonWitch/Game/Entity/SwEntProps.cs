using Prion.Node;
using SpoonWitch.Command;

namespace SpoonWitch.Game.Entity;

public class SwEntProps
{
    public readonly int Id;
    public readonly HashSet<string> Groups = [];
    private readonly Queue<SwCommand> Commands = [];
    public readonly PriDict Props = new();
    public SwEntProps()
    {
        Id = SwApp.GetNextId();
    }
    public void AddCommand(SwCommand command)
    {
        Commands.Enqueue(command);
    }
    public IEnumerable<SwCommand> GetCommands()
    {
        foreach (var item in Commands)
        {
            yield return item;
        }
    }
}