using Prion.Node;
using SpoonWitch.Game.Entity;

namespace SpoonWitch.Game.Map.MapObject;

public class SwMapTrigger : SwMapObject
{
    public SwMapTrigger(PriNode data) : base(data)
    {
    }
    public override void Load()
    {
        base.Load();
        SwGame.Game.AddEntity<SwTrigger>(GetProps());
    }
}