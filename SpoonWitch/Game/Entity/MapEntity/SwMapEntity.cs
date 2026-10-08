using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Data;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.MapEntity;

public abstract class SwMapEntity: SwEntity
{
    protected string DataPath{get; private set;} = string.Empty;
    public ErVec2 Size;
    public ErRect2I RectTiles;
    public override void Init()
    {
        base.Init();
        // load save data, if available
        if(!Props.TryGet("iid", out string iid)) {ErEngine.LogWarning("map entity missing iid"); return;}
        if(!Props.TryGet("map_iid", out string map_iid)) {ErEngine.LogWarning("map entity missing map iid"); return;}
        Size = SwPrion.GetVec2(Props.Data.Get("rect_px"), "w", "h", new ErVec2(32,32));
        RectTiles = SwPrion.GetRect2I(Props.Data.Get("rect_tiles"));
        DataPath = $"maps/{map_iid}/map_entity_data/{iid}";
        if(!SwData.SaveData.TryGet(DataPath, out PriDict saveData)) return;
        if(Props.Data is not PriDict dict) {ErEngine.LogWarning("you done goofed"); return;}
        dict.Merge(saveData);
    }
    protected virtual void Save(){}
    public void Unload()
    {
        Save();
        SwData.SaveData.TrySet(DataPath, Props.Data);
        QueueFree();
    }
}
