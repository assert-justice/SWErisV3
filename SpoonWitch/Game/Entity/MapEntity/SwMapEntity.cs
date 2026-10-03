using Eris;
using Prion.Node;
using SpoonWitch.Data;

namespace SpoonWitch.Game.Entity.MapEntity;

public abstract class SwMapEntity: SwEntity
{
    protected string DataPath{get; private set;} = string.Empty;
    public override void Init()
    {
        base.Init();
        // load save data, if available
        if(!Props.TryGet("iid", out string iid)) {ErEngine.LogWarning("map entity missing iid"); return;}
        if(!Props.TryGet("map_iid", out string map_iid)) {ErEngine.LogWarning("map entity missing map iid"); return;}
        DataPath = $"maps/{map_iid}/map_entity_data/{iid}";
        if(!SwData.SaveData.TryGet(DataPath, out PriDict saveData)) return;
        if(Props.Data is not PriDict dict) {ErEngine.LogWarning("you done goofed"); return;}
        dict.Merge(saveData);
    }
    public virtual void Unload()
    {
        SwData.SaveData.TrySet(DataPath, Props.Data);
        QueueFree();
    }
}
