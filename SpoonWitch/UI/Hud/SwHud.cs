using Eris;
using Eris.Renderer;
using ErisMath;
using SpoonWitch.Data;
using SpoonWitch.Game.Entity.Actor.Player;
using SpoonWitch.Utils;

namespace SpoonWitch.UI.Hud;

public class SwHud
{
    private readonly ErVec2 Offset;
    public SwPlayer? Player;
    private readonly ErTexture Base;
    private readonly ErVec2 BaseOff;
    public readonly SwHudItem Item;
    public readonly SwHudBar HealthBar;
    public readonly SwHudBar ManaBar;
    public readonly SwHudBar StaminaBar;
    private readonly SwHudSlots RootSlots;
    private readonly SwHudSlots AmmoSlots;
    private SwHud(ErVec2 offset)
    {
        Offset = offset;
        var node = SwData.Manifest.Get("ui/hud_config");
        var baseData = node.Get("base");
        BaseOff = SwPrion.GetVec2(baseData);
        if(!SwData.TryLoadTexture(out Base, baseData.Get("base_filepath"))) throw new("bad base path");
        if(!SwHudItem.TryLoad(out Item, offset, node)) throw new("no hud item");
        if(!SwHudBar.TryLoad(out HealthBar, offset, "health", node)) throw new("no health bar");
        if(!SwHudBar.TryLoad(out StaminaBar, offset, "stamina", node)) throw new("no stamina bar");
        if(!SwHudBar.TryLoad(out ManaBar, offset, "mana", node)) throw new("no man bar");
        if(!SwHudSlots.TryLoad(out AmmoSlots, offset, node.Get("ammo"))) throw new("no ammo");
        if(!SwHudSlots.TryLoad(out RootSlots, offset, node.Get("roots"))) throw new("no roots");
    }
    public void Update()
    {
        if(Player is null) return;
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