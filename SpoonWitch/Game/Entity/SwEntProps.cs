using Prion.Db;
using Prion.Node;
using SpoonWitch.ByteStream;

namespace SpoonWitch.Game.Entity;
// Todo: pool ent props so they can be reused without allocation
public abstract class SwEntPropsBase
{
    public readonly int Id;
    public readonly HashSet<string> Groups = [];
    private readonly Queue<PriNode> Commands = [];
    public PriDb Props{get; private set;} = new();
    public SwEntPropsBase()
    {
        Id = SwApp.GetNextId();
    }
    public SwEntPropsBase(PriNode props): this()
    {
        Props = new(props);
    }
    public void AddCommand(PriNode command)
    {
        Commands.Enqueue(command);
    }
    public IEnumerable<PriNode> GetCommands()
    {
        while(Commands.TryDequeue(out var command)) yield return command;
    }
}

public class SwEntProps<T>: SwEntPropsBase where T: SwEntity, ISwEntity<T>
{
    public readonly Type EntType = typeof(T);
    public SwEntProps(){}
    public SwEntProps(PriNode node): base(node){}
    public void Init(SwByteStream bs)
    {
        T.Primary.Init(this);
        T.Primary.Write(bs);
    }
}