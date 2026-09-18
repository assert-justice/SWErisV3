using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Entity.Actor.Player;

namespace SpoonWitch.UI.Hud;

public class SwHud
{
    private readonly ErTexture Base;
    private readonly ErVec2 BaseOff;
    public readonly SwHudItem Item;
    public readonly SwHudBar HealthBar;
    public readonly SwHudBar ManaBar;
    public readonly SwHudBar StaminaBar;
    private readonly ErVec2 Offset;
    private readonly SwHudSlots RootSlots;
    public SwPlayer? Player;
    // private readonly List<SwHudSprite> AmmoSlots = [];
    private readonly SwHudSlots AmmoSlots;
    private SwHud(ErVec2 offset)
    {
        Offset = offset;
        if(!SwApp.TryGetManJsonDirpath("hud_config", out var node, out string dirpath)) throw new("bad hud config");
        if(!node.TryGet("base", out PriNode baseData)) throw new("no base");
        if(!baseData.Get("x").TryAs(out double bx)) bx = 0;
        if(!baseData.Get("y").TryAs(out double by)) by = 0;
        BaseOff = new(bx, by);
        if(!SwApp.TryGetTex(baseData, "base_filepath", dirpath, out Base)) throw new("bad base path");
        if(!SwHudItem.TryLoad(offset, dirpath, node, out Item)) throw new("no hud item");
        if(!SwHudBar.TryLoad(offset, dirpath, "health", node, out HealthBar)) throw new("no health bar");
        if(!SwHudBar.TryLoad(offset, dirpath, "mana", node, out ManaBar)) throw new("no mana bar");
        if(!SwHudBar.TryLoad(offset, dirpath, "stamina", node, out StaminaBar)) throw new("no stamina bar");
        // if(!SwHudSprite.TryLoadList(dirpath, node.Get("roots"), RootSlots)) throw new("no roots");
        if(!SwHudSlots.TryLoad(out AmmoSlots, dirpath, node.Get("ammo"))) throw new("no ammo");
        if(!SwHudSlots.TryLoad(out RootSlots, dirpath, node.Get("roots"))) throw new("no roots");
    }
    public void Update()
    {
        if(Player is null) return;
        // foreach (var item in SwApp.CommandStore.GetCommands("hud_set"))
        // {
        //     TryHandleSet(item);
        // }
        HealthBar.MaxValue = Player.MaxHealth;
        HealthBar.Value = Player.Health;
        HealthBar.Update();
        StaminaBar.MaxValue = Player.MaxStamina;
        StaminaBar.Value = Player.Stamina;
        StaminaBar.Update();
        ManaBar.MaxValue = Player.MaxMana;
        ManaBar.Value = Player.Mana;
        ManaBar.Update();
        RootSlots.MaxValue = Player.MaxRoots;
        RootSlots.Value = Player.Roots;
        RootSlots.Update();
        AmmoSlots.MaxValue = Player.MaxAmmo;
        AmmoSlots.Value = Player.Ammo;
        AmmoSlots.Update();
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
            case "health_max":
                HealthBar.MaxValue = value;
                break;
            case "stamina":
                StaminaBar.Value = value;
                break;
            case "stamina_max":
                StaminaBar.MaxValue = value;
                break;
            case "mana":
                ManaBar.Value = value;
                break;
            case "mana_max":
                ManaBar.MaxValue = value;
                break;
            case "sling_ammo":
                AmmoSlots.Value = (int)value;
                break;
            case "sling_ammo_max":
                AmmoSlots.MaxValue = (int)value;
                break;
            case "roots":
                AmmoSlots.Value = (int)value;
                break;
            case "roots_max":
                AmmoSlots.MaxValue = (int)value;
                break;
            default:
                return ErEngine.LogWarning("invalid hud key '", key, "'");
        }
        return true;
    }
    public void Draw()
    {
        Base.Draw(Offset + BaseOff);
        Item.Draw();
        HealthBar.Draw();
        ManaBar.Draw();
        StaminaBar.Draw();
        RootSlots.Draw();
        AmmoSlots.Draw();
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