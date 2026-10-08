using Eris;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.MapEntity;

public class SwCheckpoint: SwMapEntity
{
    public bool IsEnabled{get; private set;}
    private SwSprite? Sprite;
    private SwAreaComponent? Area;
    public override void Init()
    {
        base.Init();
        LoadSprites("sprites");
        Sprite = GetComponent<SwSpriteComponent>("sprite")?.Sprite;
        Area = new(this, "area", (uint)SwCollisionMask.PlayerTeam, Size, enabled:true, onBodyEnter: OnEnter);
        RegisterComponent(Area);
        AddGlobalHandler("map_set_checkpoint", SetCheckpoint);
        if(Game.Map.CurrentCheckpoint.Iid == Iid) SetEnabled(true);
    }
    private void SetCheckpoint(PriNode command)
    {
        bool isEnabled = command.TryGet("iid", out string iid) && iid == Iid;
        SetEnabled(isEnabled);
    }
    private void SetEnabled(bool isEnabled)
    {
        if(isEnabled == IsEnabled) return;
        IsEnabled = isEnabled;
        if (isEnabled)
        {
            // set sprite to enabled
            Sprite?.Play("active");
        }
        else
        {
            // set sprite to default
            Sprite?.Play("default");
        }
    }
    private void OnEnter(SwEntity entity)
    {
        PriDict command = [];
        command.TrySet("verb", "map_set_checkpoint");
        command.TrySet("iid", Iid);
        SwApp.CommandQueue.AddCommand(command);
        command = [];
        command.TrySet("verb", "enter_checkpoint");
        command.TrySet("iid", Iid);
        entity.AddCommand(command);
    }
}
