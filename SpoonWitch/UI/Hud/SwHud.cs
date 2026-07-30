using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;

namespace SpoonWitch.UI.Hud;

public class SwHud
{
    // private readonly ErTexture Texture = ErTexture.GetColoredTexture(SwApp.INTERNAL_WIDTH,SwApp.HUD_HEIGHT, ErColor.White);
    private readonly ErTexture Base;
    private readonly ErVec2 BaseOff;
    public readonly SwHudItem Item;
    public readonly SwHudBar HealthBar;
    public readonly SwHudBar ManaBar;
    public readonly SwHudBar StaminaBar;
    private readonly ErVec2 Offset;
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
    }
    public void Update(){}
    public void Draw()
    {
        Base.Draw(Offset + BaseOff);
        Item.Draw();
        HealthBar.Draw();
        ManaBar.Draw();
        StaminaBar.Draw();
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