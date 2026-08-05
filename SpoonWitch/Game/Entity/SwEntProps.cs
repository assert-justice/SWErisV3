using Prion.Node;
using SpoonWitch.Command;

namespace SpoonWitch.Game.Entity;

public class SwEntProps
{
    public readonly int Id;
    public readonly HashSet<string> Groups = [];
    private readonly Queue<SwCommand> Commands = [];
    public readonly PriNode Props = PriNull.Null;
    public SwEntProps()
    {
        Id = SwApp.GetNextId();
    }
    public SwEntProps(PriNode props): this()
    {
        Props = props;
    }
    public void AddCommand(SwCommand command)
    {
        Commands.Enqueue(command);
    }
    public IEnumerable<SwCommand> GetCommands()
    {
        while(Commands.TryDequeue(out var command)) yield return command;
    }
}