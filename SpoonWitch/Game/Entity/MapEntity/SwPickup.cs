using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Data;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Utils;

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
    // public int Max
    // {
    //     get => Props.TryGet("fields/max", out int i) ? i : 0;
    //     set => Props.TrySet("fields/max", value);
    // }
    public SwPickup()
    {
        AddHandler("pickup_set_rem", SetRem);
    }
    public override void SetProps(PriNode props)
    {
        base.SetProps(props);
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
    private void SetRem(PriNode command)
    {
        if(!command.TryGet("rem", out int rem)) return;
        Props.TrySet("count", rem);
        Count = rem;
        if(rem == 0) Area.Enabled = false;
    }
    private void OnEnter(SwEntity entity)
    {
        PriDict command = [];
        command.TrySet("verb", "ent_offer_item");
        command.TrySet("ent_id", Id);
        command.TrySet("pickup_type", PickupType);
        command.TrySet("count", Count);
        command.Add("max", Props.Get("fields/max"));
        entity.AddCommand(command);
    }
}
