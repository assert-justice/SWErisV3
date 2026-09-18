using Eris;
using Eris.Renderer;
using ErisMath;
using ErisPhysics2D.Collider;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Map.Collision;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity;

public class SwPickup : SwEntity
{
    private readonly SwAreaComponent Area;
    private ErTexture? Texture;
    public SwPickup()
    {
        Area = new(this, "area", 2, new(32, 32), enabled: true, onBodyEnter: OnEnter);
        RegisterComponent(Area);
        AddHandler("pickup_set_rem", SetRem);
    }
    public override void Ready()
    {
        base.Ready();
        PriDict command = [];
        command.TrySet("verb", "ent_offer_item");
        command.TrySet("pickup_type", Props.Get("pickup_type"));
        command.TrySet("count", Props.Get("count"));
        command.TrySet("ent_id", Id);
        Props.TrySet("ent_offer_item", command);
        // Todo: obviously don't hardcode this
        string texture_filepath = "game_data/entities/actors/player/images/bella_sling_ammo_pickup.png";
        if(!ErTexture.TryFromPath(texture_filepath, out Texture)) return;
        var size = SwPrion.GetVec2(Props.Data, "width_px", "height_px");
        Area.Size = size;
        if(Props.TryGet("mask", out uint mask)) Area.Mask = mask;
    }
    protected override void DrawImpl(SwEntity nextState)
    {
        base.DrawImpl(nextState);
        if(!Props.TryGet("count", out int count)) return;
        if(Texture is null) return;
        var center = Texture.Size * 0.5;
        for (int idx = 0; idx < count; idx++)
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
        if(rem == 0) Area.Enabled = false;
    }
    private void OnEnter(SwEntity entity)
    {
        if(!Props.TryGet("ent_offer_item", out PriNode command)) return;
        entity.AddCommand(command);
    }
}