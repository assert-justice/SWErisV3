using Eris;
using Eris.Renderer;
using ErisMath;
using ErisPhysics2D.Collider;
using Prion.Node;
using SpoonWitch.Game.Inventory;
using SpoonWitch.Game.Map.Collision;

namespace SpoonWitch.Game.Map.MapObject;

public class SwMapPickup : SwMapObject
{
    public readonly string PickupType = "";
    public readonly int IntId;
    private readonly SwColliderArea Area;
    private int _Count = 0;
    public int Count
    {
        get => _Count;
        set
        {
            while(_Count < value)
            {
                AddSprite();
                _Count++;
            }
            while(_Count > value)
            {
                SpritePositions.TryPop(out _);
            }
        }
    }
    public readonly bool Visible;
    private readonly Stack<ErVec2> SpritePositions = [];
    private readonly ErTexture? Texture;
    public SwMapPickup(PriNode data) : base(data)
    {
        if(Fields.TryGet("pickup_type", out string pickup_type)) PickupType = pickup_type;
        if(Fields.TryGet("count", out int count)) _Count = count;
        IntId = SwApp.GetNextId();
        SwInventory inventory = new();
        inventory.SetCount(PickupType, Count);
        SwGame.InventoryLookup.Add(IntId, inventory);
        Area = new()
        {
            ParentId = IntId,
            Mask = 2,
            Rect = RectPx,
        };
        Visible = Fields.TryGet("visible", out bool visible) && visible;
        if(!Visible) return;
        if(!data.TryGet("dirpath", out string dirpath)) throw new("missing dirpath");
        if(!Fields.TryGet("texture_filepath", out string filepath)) throw new("missing texture");
        if(!ErTexture.TryFromPath(Path.Join(dirpath, filepath), out var texture)) throw new("could not load texture");
        Texture = texture;
        _Count = 0;
        Count = count;
    }
    public override void Draw()
    {
        base.Draw();
        if(!Visible || Texture is null) return;
        foreach (var item in SpritePositions)
        {
            Texture.Draw(item);
        }
        ErEngine.Renderer.DebugDrawRect(ErColor.Green, RectPx, false);
    }
    private void AddSprite()
    {
        if(Texture is null) return;
        var size = (RectPx.Size - Texture.Size) * 0.25;
        if(size.X < 0 || size.Y < 0) return;
        double x = (Random.Shared.NextDouble() * 2 - 1) * size.X;
        double y = (Random.Shared.NextDouble() * 2 - 1) * size.Y;
        SpritePositions.Push(new ErVec2(x,y) - Texture.Size * 0.5 + RectPx.Center);
    }
    private static void OnEnter(SwColliderArea area,int bodyId,ErColliderBody body)
    {
        PriDict command = [];
        command.TrySet("verb", "ent_offer_items");
        // command.TrySet("pickup_type", )
    }
}
