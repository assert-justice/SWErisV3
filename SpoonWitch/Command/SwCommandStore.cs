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
    private class SwQueue
    {
        private readonly Queue<PriNode> Commands = [];
        private long LastUsed;
        private void NoteUsed()
        {
            LastUsed = DateTime.UtcNow.Ticks;
        }
        public IEnumerable<PriNode> GetCommands()
        {
            if(Commands.Count > 0) NoteUsed();
            while(Commands.TryDequeue(out var command)) yield return command;
        }
        public bool CanEvict(long now, long ageGate)
        {
            if(Commands.Count > 0) return false;
            return now - LastUsed > ageGate;
        }
        public void AddCommand(PriNode command)
        {
            Commands.Enqueue(command);
            NoteUsed();
        }
    }
    private readonly Dictionary<string, SwStore> GeneralStores = [];
    private readonly Dictionary<string,SwQueue> QueueLookup = [];
    public IEnumerable<PriNode> GetGlobalCommands(string verb)
    {
        if(!GeneralStores.TryGetValue(verb, out var store)) return [];
        else return store.GetCommands();
    }
    public IEnumerable<PriNode> HandleCommands(string id)
    {
        if(!QueueLookup.TryGetValue(id, out var commands)) return [];
        return commands.GetCommands();
    }
    public void AddGlobalCommand(PriNode command)
    {
        if(!command.TryGet("verb", out string verb))
        {
            ErEngine.LogWarning("malformed command, missing verb");
            return;
        }
        if(!GeneralStores.TryGetValue(verb, out var store))
        {
            store = new();
            GeneralStores[verb] = store;
        }
        store.AddCommand(command);
    }
    public void AddCommand(string id, PriNode command)
    {
        if(!QueueLookup.TryGetValue(id, out var queue))
        {
            queue = new();
            QueueLookup.Add(id, queue); 
        }
        queue.AddCommand(command);
    }
    public void Flush()
    {
        foreach (var item in GeneralStores.Values)
        {
            item.Flush();
        }
    }
}