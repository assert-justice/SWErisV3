using Eris;
using Eris.Renderer;
using ErisMath;
using ErisPhysics2D.Collider;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Map.Collision;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity;

public class SwPickup : SwEntity, ISwEntity<SwPickup>
{
    public static byte TypeId => 5;
    public override uint Mask => 0;
    private static SwPickup? _Primary;
    private static SwPickup? _Secondary;
    public static SwPickup Primary => _Primary ??= new();
    public static SwPickup Secondary => _Secondary ??= new();
    protected override byte GetTypeId => TypeId;
    private readonly SwAreaComponent Area;
    private readonly List<ErTexture> Textures = [];
    private readonly Dictionary<string,int> TextureLookup = [];
    public SwPickup()
    {
        Area = new(this, "area", 2, new(32, 32))
        {
            Enabled = true,
        };
        Area.Area.OnBodyEnterFn = OnEnter;
        RegisterComponent(Area);
        AddHandler("pickup_set_rem", SetRem);
    }
    public override void Ready()
    {
        base.Ready();
        PriDict command = [];
        command.TrySet("verb", "ent_offer_item");
        command.TrySet("pickup_type", EntProps.Props.Get("pickup_type"));
        command.TrySet("count", EntProps.Props.Get("count"));
        command.TrySet("ent_id", Id);
        EntProps.Props.TrySet("ent_offer_item", command);
        // Todo: obviously don't hardcode this
        string texture_filepath = "game_data/entities/actors/player/images/bella_sling_ammo_pickup.png";
        if(!TextureLookup.TryGetValue(texture_filepath, out int texId))
        {
            texId = Textures.Count;
            if(!ErTexture.TryFromPath(texture_filepath, out var texture)) return;
            Textures.Add(texture);
            TextureLookup[texture_filepath] = texId;
        }
        EntProps.Props.TrySet("tex_id", texId);
        var size = SwPrion.GetVec2(EntProps.Props.Data, "width_px", "height_px");
        Area.Size = size;
        if(EntProps.Props.TryGet("mask", out uint mask)) Area.Mask = mask;
    }
    protected override void DrawImpl(SwEntity nextState)
    {
        base.DrawImpl(nextState);
        if(!SwGame.TryGetEntProps(Id, out var entProps)) return;
        if(!entProps.Props.TryGet("count", out int count)) return;
        if(!entProps.Props.TryGet("tex_id", out int texId)) return;
        var center = Textures[texId].Size * 0.5;
        for (int idx = 0; idx < count; idx++)
        {
            double dis = idx * center.X;
            double angle = idx * ErMath.TAU / 6;
            ErVec2 vec = ErVec2.FromAngle(angle) * dis;
            Textures[texId].Draw(Position + vec - center);
        }
    }
    private void SetRem(PriNode command)
    {
        if(!command.TryGet("rem", out int rem)) return;
        EntProps.Props.TrySet("count", rem);
        if(rem == 0) Area.Enabled = false;
    }
    private static void OnEnter(SwColliderArea area, int bodyId, ErColliderBody body)
    {
        if(!SwGame.TryGetEntProps(area.ParentId, out var pickupProps)) return;
        if(!SwGame.TryGetEntProps(body.ParentId, out var targetProps)) return;
        if(!pickupProps.Props.TryGet("ent_offer_item", out PriNode command)) return;
        targetProps.AddCommand(command);
    }
}