using Eris;
using ErisMath;
using Prion.Node;

namespace SpoonWitch.Game.Map.MapObject;

public class SwMapCheckpoint : SwMapObject
{
    public override bool IsGlobal => true;
    public SwMapCheckpoint(PriNode data) : base(data){}
    public override void Trigger()
    {
        base.Trigger();
        var props = GetProps();
        props.TrySet("verb", "game_respawn_player");
        SwApp.CommandStore.AddCommand(props);
    }
}