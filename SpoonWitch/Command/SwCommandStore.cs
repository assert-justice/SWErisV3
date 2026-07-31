using Eris;

namespace SpoonWitch.Command;

public class SwCommandStore
{
    // private class SwStore
    // {
    //     private readonly List<SwCommand> Commands = [];
    //     private readonly Queue<SwCommand> Overflow = [];
    //     public IEnumerable<SwCommand> GetCommands()
    //     {
    //         foreach (var item in Commands)
    //         {
    //             yield return item;
    //         }
    //     }
    //     public void AddCommand(SwCommand command)
    //     {
    //         Overflow.Enqueue(command);
    //     }
    //     public void Flush()
    //     {
    //         Commands.Clear();
    //         while(Overflow.TryDequeue(out var command)) Commands.Add(command);
    //     }
    // }
    private readonly List<SwCommand> Commands = [];
    private readonly Queue<SwCommand> Overflow = [];
    public IEnumerable<SwCommand> GetCommands()
    {
        foreach (var item in Commands)
        {
            yield return item;
        }
    }
    public void AddCommand(SwCommand command)
    {
        Overflow.Enqueue(command);
    }
    public void Flush()
    {
        Commands.Clear();
        while(Overflow.TryDequeue(out var command)) Commands.Add(command);
    }

    // private readonly Dictionary<string, SwStore> GeneralStores = [];
    // private readonly Dictionary<int,Dictionary<string, SwStore>> TargetedStores = [];
    // private bool TryGetStoreLookup(int targetId, out Dictionary<string, SwStore> storeLookup)
    // {
    //     Dictionary<string, SwStore>? lookup = null;
    //     storeLookup = default!;
    //     if(targetId < 0) storeLookup = GeneralStores;
    //     else if(!TargetedStores.TryGetValue(targetId, out lookup)) return false;
    //     if(lookup is null) return false;
    //     storeLookup = lookup;
    //     return true;
    // }
    // public IEnumerable<SwCommand> GetCommands(string verb, int targetId = -1)
    // {
    //     if(!TryGetStoreLookup(targetId, out var storeLookup)) return [];
    //     else if(!storeLookup.TryGetValue(verb, out var store)) return [];
    //     else return store.GetCommands();
    // }
    // public void AddCommand(SwCommand command)
    // {
    //     Dictionary<string, SwStore> storeLookup;
    //     if(command.TargetId < 0) storeLookup = GeneralStores;
    //     else if(!TryGetStoreLookup(command.TargetId, out storeLookup))
    //     {
    //         storeLookup = [];
    //         TargetedStores.Add(command.TargetId, storeLookup);
    //     }
    //     if(!storeLookup.TryGetValue(command.Verb, out var store))
    //     {
    //         store = new();
    //         storeLookup.Add(command.Verb, store);
    //     }
    //     store.AddCommand(command);
    // }
    // public void Flush()
    // {
    //     foreach (var item in GeneralStores.Values)
    //     {
    //         item.Flush();
    //     }
    //     foreach (var item in TargetedStores.Values)
    //     {
    //         foreach (var store in item.Values)
    //         {
    //             store.Flush();
    //         }
    //     }
    // }
}