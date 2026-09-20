using Eris;
using Prion.Node;

namespace SpoonWitch.Command;

public class SwCommandStore
{
    private class SwStore
    {
        private readonly List<PriNode> Commands = [];
        private readonly Queue<PriNode> Overflow = [];
        public IEnumerable<PriNode> GetCommands()
        {
            foreach (var item in Commands)
            {
                yield return item;
            }
        }
        public void AddCommand(PriNode command)
        {
            Overflow.Enqueue(command);
        }
        public void Flush()
        {
            Commands.Clear();
            while(Overflow.TryDequeue(out var command)) Commands.Add(command);
        }
    }
    private readonly Dictionary<string, SwStore> Stores = [];
    public IEnumerable<PriNode> GetCommands(string verb)
    {
        if(!Stores.TryGetValue(verb, out var store)) return [];
        else return store.GetCommands();
    }
    public void AddCommandVerb(string verb)
    {
        PriDict command = [];
        command.TrySet("verb", verb);
        AddCommand(command);
    }
    public void AddCommand(PriNode command)
    {
        if(command is PriNull) return;
        if(command is PriList list)
        {
            foreach (var item in list.Data)
            {
                AddCommand(item);
            }
            return;
        }
        if(!command.TryGet("verb", out string verb))
        {
            ErEngine.LogWarning("malformed command, missing verb");
            return;
        }
        if(!Stores.TryGetValue(verb, out var store))
        {
            store = new();
            Stores[verb] = store;
        }
        store.AddCommand(command);
    }
    public void Flush()
    {
        foreach (var item in Stores.Values)
        {
            item.Flush();
        }
    }
}