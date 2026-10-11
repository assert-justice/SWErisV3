using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Data;
using SpoonWitch.Game.Entity.Component;

namespace SpoonWitch.Game.Entity.MapEntity;

public class SwPickup : SwMapEntity
{
    private SwAreaComponent Area = null!;
    private ErTexture? Texture;
    private string PickupType = string.Empty;
    public int Count
    {
        get => Props.TryGet("fields/count", out int i) ? i : 0;
        set => Props.TrySet("fields/count", value);
    }
    public override void Init()
    {
        base.Init();
        if(Props.TryGet("fields/pickup_type", out string s)) PickupType = s;
        var pickupData = SwData.Prototypes.Get($"pickups/{PickupType}");
        Props.TrySet("pickup", pickupData);
        SwData.TryLoadTexture(out Texture, Props.Get("pickup/texture_filepath"));
        if(!Props.TryGet("fields/mask", out uint mask)) mask = 4;
        Area = new(this, "area", mask, Size, enabled: Count > 0, onBodyEnter: OnEnter);
        RegisterComponent(Area);
    }
    protected override void Draw()
    {
        base.Draw();
        if(Texture is null) return;
        var center = Texture.Size * 0.5;
        for (int idx = 0; idx < Count; idx++)
        {
            double dis = idx * center.X;
            double angle = idx * ErMath.TAU / 6;
            ErVec2 vec = ErVec2.FromAngle(angle) * dis;
            Texture.Draw(Position + vec - center);
        }
    }
    private void OnEnter(SwEntity _)
    {
        PriDict command = [];
        command.TrySet("verb", "player_item_inc");
        command.TrySet("pickup_type", PickupType);
        command.TrySet("count", Count);
        SwApp.CommandQueue.AddCommand(command);
        Count = 0;
        Area.Enabled = false;
    }
}
