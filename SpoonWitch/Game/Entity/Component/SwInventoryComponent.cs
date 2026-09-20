using SpoonWitch.ByteStream;
using SpoonWitch.Game.Inventory;

namespace SpoonWitch.Game.Entity.Component;

public class SwInventoryComponent: SwComponent
{
    private int Id;
    public SwInventory Entries = null!;
    public SwInventoryComponent(SwEntity parent, string name) : base(parent, name)
    {
    }
    public override void Ready()
    {
        base.Ready();
        Id = SwApp.GetNextId();
        Entries = new();
    }
}
