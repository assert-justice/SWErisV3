using Eris;
using Prion.Node;

namespace SpoonWitch.Game.Map.MapObject;

public class SwMapSpawner : SwMapObject
{
    public readonly string EntityType;
    public readonly bool TriggerOnLoad;
    public readonly int? MaxUses;
    public int TimesUsed{get; private set;} = 0;
    public SwMapSpawner(PriNode data) : base(data)
    {
        EntityType = "none";
        if(!Fields.TryGet("entity_type", out string entType)) ErEngine.LogWarning("no entity type supplied");
        else EntityType = entType;
        if(!Fields.TryGet("trigger_on_load", out TriggerOnLoad)) TriggerOnLoad = false;
        if(!Fields.TryGet("max_uses", out int maxUses)) MaxUses = null;
        else MaxUses = maxUses;
    }
    public override void Load()
    {
        base.Load();
        if(TriggerOnLoad) Trigger();
    }
    private bool CanTrigger()
    {
        if(MaxUses is null) return true;
        if(TimesUsed < MaxUses) return true;
        return false;
    }
    public override void Trigger()
    {
        if(!CanTrigger()) return;
        base.Trigger();
        PriNode props = GetProps();
        props.TrySet("verb", "spawn_entity");
        props.TrySet("entity_type", EntityType);
        SwApp.CommandStore.AddGlobalCommand(props);
    }
}