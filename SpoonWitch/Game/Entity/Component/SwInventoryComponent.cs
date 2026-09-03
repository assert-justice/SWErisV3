using SpoonWitch.ByteStream;
using SpoonWitch.Game.Inventory;

namespace SpoonWitch.Game.Entity.Component;

public class SwInventoryComponent: SwComponent
{
    private int Id;
    public SwInventory? Entries;
    public SwInventoryComponent(SwEntity parent, string name) : base(parent, name)
    {
    }
    public override void Ready()
    {
        base.Ready();
        Id = SwApp.GetNextId();
        Entries = new();
        SwGame.InventoryLookup.Add(Id, Entries);
    }
    public override void Read(SwByteStream byteStream)
    {
        base.Read(byteStream);
        byteStream.TryReadI32(out Id);
        if(!SwGame.InventoryLookup.TryGetValue(Id, out var inventory)) return;
        Entries = inventory;
    }
    public override void Write(SwByteStream byteStream)
    {
        base.Write(byteStream);
        byteStream.WriteI32(Id);
        Entries = null;
    }
}
