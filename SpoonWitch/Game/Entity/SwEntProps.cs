using Prion.Node;
using SpoonWitch.ByteStream;
using SpoonWitch.Command;

namespace SpoonWitch.Game.Entity;

public abstract class SwEntPropsBase
{
    public readonly int Id;
    public readonly HashSet<string> Groups = [];
    private readonly Queue<PriNode> Commands = [];
    public PriNode Props{get; private set;} = PriNull.Null;
    public SwEntPropsBase()
    {
        Id = SwApp.GetNextId();
    }
    public SwEntPropsBase(PriNode props): this()
    {
        Props = props;
    }
    public void AddCommand(PriNode command)
    {
        Commands.Enqueue(command);
    }
    public IEnumerable<PriNode> GetCommands()
    {
        while(Commands.TryDequeue(out var command)) yield return command;
    }
    public void Set(string key, PriNode node)
    {
        if(!Props.TrySet(key, node))
        {
            PriDict dict = new();
            dict.Data[key]= node;
            Props = dict;
        }
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