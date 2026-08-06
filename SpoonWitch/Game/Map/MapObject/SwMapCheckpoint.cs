using Eris;
using ErisMath;
using Prion.Node;

namespace SpoonWitch.Game.Map.MapObject;

public class SwMapCheckpoint : SwMapObject
{
    public override bool IsGlobal => true;
    // public override bool IsGlobal => base.IsGlobal || (Fields.TryGet("default", out bool isDefault) && isDefault);
    public SwMapCheckpoint(PriNode data) : base(data)
    {
    }
    public override void Trigger()
    {
        base.Trigger();
        // Spawn player
        // ErEngine.Log("spawn player");
        // PriDict props = new();
        // var center = RectPx.Center;
        // props.Data["x_px"] = new PriNumber(center.X);
        // props.Data["y_px"] = new PriNumber(center.X);
        // if(Fields.TryGet("property_overrides", out PriDict dict))
        // {
        //     foreach (var (key,val) in dict.Data)
        //     {
        //         props.Data[key] = val;
        //     }
        // }
        SwApp.CommandStore.AddGlobalCommand(new("spawn_player", GetProps()));
    }
}