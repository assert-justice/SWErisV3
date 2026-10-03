using Eris;
using Prion.Node;
using SpoonWitch.Data;
using SpoonWitch.Game.Entity.Actor.Enemy.Aspect;
using SpoonWitch.Game.Entity.Actor.Enemy.Knight;
using SpoonWitch.Game.Entity.Actor.Enemy.Slume;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.MapEntity;

public class SwSpawner: SwMapEntity
{
    private readonly List<string> SpawnedEntIds = [];
    public int Activations
    {
        get => Props.TryGet("activations", out int activations) ? activations : 0;
        set => Props.TrySet("activations", value);
    }
    public override void Init()
    {
        base.Init();
        foreach (var item in Props.Get("spawned_entities").Values)
        {
            Spawn(item);
        }
        Props.TrySet("spawned_entities", new PriDict());
        if(Props.TryGet("fields/trigger_on_load", out bool b)) SpawnNew();
    }
    protected override void Save()
    {
        base.Save();
        // save and unload living spawned entities
        PriDict spawnedEntities = [];
        foreach (var id in SpawnedEntIds)
        {
            if(!Game.EntityLookup.TryGet(id, out SwEntity entity)) continue;
            spawnedEntities.Add(id, entity.Props.Data);
            entity.QueueFree();
        }
        if(spawnedEntities.Count > 0)
        {
            Props.TrySet("spawned_entities", spawnedEntities);
        }
    }
    private bool CanSpawn()
    {
        if(!Props.TryGet("fields/max_uses", out int maxUses)) maxUses = 1;
        if(maxUses < 0) return true;
        return Activations < maxUses;
    }
    private void SpawnNew()
    {
        if(!CanSpawn()) return;
        if(!Props.TryGet("fields/entity_type", out string entType)) return;
        Activations++;
        var entProps = SwData.Prototypes.Get($"entities/{entType}").DeepCopy();
        SwPrion.TrySetVec2(entProps, Position);
        entProps.TrySet("is_passive", Props.Get("fields/is_passive"));
        entProps.TrySet("ent_type", entType);
        Spawn(entProps);
    }
    private void Spawn(PriNode entProps)
    {
        if(!entProps.TryGet("ent_type", out string entType)){ErEngine.LogWarning("spawn props missing ent type"); return;}
        int entId;
        switch (entType)
        {
            case "slume":
                entId = Game.AddEntity<SwSlume>(entProps).Id;
                break;
            case "knight":
                entId = Game.AddEntity<SwKnight>(entProps).Id;
                break;
            case "aspect":
                entId = Game.AddEntity<SwAspect>(entProps).Id;
                break;
            default:
                ErEngine.LogWarning("unsupported spawn type '", entType, "'");
                return;
        }
        SpawnedEntIds.Add(entId.ToString());
    }
}
