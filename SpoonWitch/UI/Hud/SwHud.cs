using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;

namespace SpoonWitch.UI.Hud;

public class SwHud
{
    private readonly ErTexture Texture = ErTexture.GetColoredTexture(SwApp.INTERNAL_WIDTH,SwApp.HUD_HEIGHT, new(100, 100, 100));
    private readonly ErTexture Base;
    private readonly ErVec2 BaseOff;
    public readonly SwHudItem Item;
    public readonly SwHudBar HealthBar;
    public readonly SwHudBar ManaBar;
    public readonly SwHudBar StaminaBar;
    private readonly ErVec2 Offset;
    private readonly List<SwHudSprite> RootSlots = [];
    private readonly List<SwHudSprite> AmmoSlots = [];
    private SwHud(ErVec2 offset)
    {
        string path = "game_data/hud/hud_config.json";
        string dirpath = Path.GetDirectoryName(path)!;
        Offset = offset;
        if(!SwApp.TryLoadPrion(path, out var node)) throw new("bad hud config");
        if(!node.Get("base").TryAs(out PriNode baseData)) throw new("no base");
        if(!baseData.Get("base_filename").TryAs(out string base_filename)) throw new("no base filename");
        if(!baseData.Get("x").TryAs(out double bx)) bx = 0;
        if(!baseData.Get("y").TryAs(out double by)) by = 0;
        BaseOff = new(bx, by);
        if(!ErTexture.TryFromPath(Path.Join(dirpath, base_filename), out Base)) throw new("bad base path");
        if(!SwHudItem.TryLoad(offset, dirpath, node, out Item)) throw new("no hud item");
        if(!SwHudBar.TryLoad(offset, dirpath, "health", node, out HealthBar)) throw new("no health bar");
        if(!SwHudBar.TryLoad(offset, dirpath, "mana", node, out ManaBar)) throw new("no mana bar");
        if(!SwHudBar.TryLoad(offset, dirpath, "stamina", node, out StaminaBar)) throw new("no stamina bar");
        if(node.TryGet("root_slots", out PriList rootSlots))
        {
            foreach (var item in rootSlots.Values)
            {
                if(!item.TryGet("x", out double x)) x = 0;
                if(!item.TryGet("y", out double y)) y = 0;
                if(!SwHudSprite.TryLoad(new(x,y), "game_data/hud/vitality_root_slot.png", out var rootSlot)) throw new("bad root slot");
                RootSlots.Add(rootSlot);
            }
        }
        if(node.TryGet("ammo_slots", out PriList ammoSlots))
        {
            foreach (var item in ammoSlots.Values)
            {
                if(!item.TryGet("x", out double x)) x = 0;
                if(!item.TryGet("y", out double y)) y = 0;
                if(!SwHudSprite.TryLoad(new(x,y), "game_data/hud/sling_ammo_slot.png", out var ammoSlot)) throw new("bad root slot");
                AmmoSlots.Add(ammoSlot);
            }
        }
    }
    public void Update()
    {
        foreach (var item in SwApp.CommandStore.GetGlobalCommands("hud_set"))
        {
            TryHandleSet(item.Payload);
        }
        foreach (var item in RootSlots)
        {
            item.Update();
        }
        foreach (var item in AmmoSlots)
        {
            item.Update();
        }
    }
    private bool TryHandleSet(PriNode node)
    {
        if(!node.TryGet("key", out string key)) return ErEngine.LogWarning("hud set payload missing key");
        if(!node.TryGet("value", out double value)) return ErEngine.LogWarning("hud set payload missing value");
        if(value < 0) value = 0;
        switch (key)
        {
            case "health":
            HealthBar.Value = value;
            break;
            default:
            return ErEngine.LogWarning("invalid hud key '", key, "'");
        }
        return true;
    }
    public void Draw()
    {
        Texture.Draw(ErVec2.Zero);
        Base.Draw(Offset + BaseOff);
        Item.Draw();
        HealthBar.Draw();
        ManaBar.Draw();
        StaminaBar.Draw();
        foreach (var item in RootSlots)
        {
            item.Draw();
        }
        foreach (var item in AmmoSlots)
        {
            item.Draw();
        }
    }
    public static bool TryLoad(ErVec2 offset, out SwHud hud)
    {
        hud = default!;
        try
        {
            hud = new(offset);
            return true;
        }
        catch(Exception e)
        {
            return ErEngine.LogWarning(e);
        }
    }
}