using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Data;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity;

public class SwPickup : SwEntity
{
    private SwAreaComponent Area = null!;
    private ErTexture? Texture;
    public int Count;
    public int MaxUses = 0;
    public int Uses = 0;
    public SwPickup()
    {
        AddHandler("pickup_set_rem", SetRem);
    }
    protected override void SetProps(PriNode props)
    {
        base.SetProps(props);
        if(Props.TryGet("count", out int i)) Count = i;
        if(Props.TryGet("max_uses", out i)) MaxUses = i;
    }
    public override void Init()
    {
        base.Init();
        SwData.TryLoadTexture(out Texture, Props.Get("texture_filepath"));
        if(!Props.TryGet("mask", out uint mask)) mask = 2;
        Area = new(this, "area", mask, SwPrion.GetVec2(Props.Data, "width_px", "height_px", new ErVec2(32,32)), enabled: true, onBodyEnter: OnEnter);
        RegisterComponent(Area);
    }
    protected override void DrawImpl(SwEntity nextState)
    {
        base.DrawImpl(nextState);
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
        if(Props.Get("on_enter").DeepCopy().TryAs(out PriDict command))
        {
            if(!command.TryGet("verb", out string verb)) return;
            switch (verb)
            {
                case "ent_offer_item":
                    command.TrySet("ent_id", Id);
                    command.TrySet("count", Count);
                    break;
                default:
                    break;
            }
            entity.AddCommand(command);
        }
        if(Props.TryGet("on_enter_global", out command)) SwApp.CommandStore.AddCommand(command);
        Uses++;
        if(MaxUses > 0 && Uses >= MaxUses)
        {
            Area.Enabled = false;
            Visible = false;
        }
    }
}