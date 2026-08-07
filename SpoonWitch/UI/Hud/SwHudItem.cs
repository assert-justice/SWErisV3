using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Entity.Component.Sprite;

namespace SpoonWitch.UI.Hud;

public class SwHudItem
{
    private readonly ErVec2 Offset;
    private readonly ErTexture Frame;
    private readonly SwSpriteAnimation Icons;
    private readonly ErVec2 IconOff;
    private readonly SwSpriteAnimation Digits;
    private readonly ErVec2 TensOff;
    private readonly ErVec2 OnesOff;
    public int ItemIdx = 2;
    public int MaxQuantity;
    public int Quantity = 69;
    private SwHudItem(ErVec2 offset, string dirpath, PriNode node)
    {
        var icon = node.Get("item_slot");
        if(!icon.Get("x").TryAs(out double x)) x = 0;
        if(!icon.Get("y").TryAs(out double y)) y = 0;
        Offset = offset + new ErVec2(x, y);
        if(!icon.TryGet("icon_x", out double ix)) ix = 0;
        if(!icon.TryGet("icon_y", out double iy)) iy = 0;
        if(!icon.TryGet("icon_width", out double icon_width)) icon_width = 32;
        if(!icon.TryGet("icon_height", out double icon_height)) icon_height = 32;
        IconOff = new(ix, iy);
        if(!icon.TryGet("tens_digit_x", out double tx)) tx = 0;
        if(!icon.TryGet("tens_digit_y", out double ty)) ty = 0;
        TensOff = new(tx, ty);
        if(!icon.TryGet("ones_digit_x", out double ox)) ox = 0;
        if(!icon.TryGet("ones_digit_y", out double oy)) oy = 0;
        OnesOff = new(ox, oy);
        if(!icon.TryGet("digit_width", out double digit_width)) digit_width = 32;
        if(!icon.TryGet("digit_height", out double digit_height)) digit_height = 32;
        if(!icon.TryGet("frame_filepath", out string filename)) throw new("bad frame file");
        if(!ErTexture.TryFromPath(Path.Join(dirpath,filename), out Frame)) throw new("bad frame");
        if(!icon.TryGet("icons_filepath", out filename)) throw new("bad icon file");
        if(!ErTexture.TryFromPath(Path.Join(dirpath,filename), out var tex)) throw new("bad icon");
        if(!SwSpriteAnimation.TryFromTexture(tex, new(icon_width,icon_height), out Icons)) throw new("poo");
        if(!icon.Get("digits_filepath").TryAs(out filename)) throw new("bad digits file");
        if(!ErTexture.TryFromPath(Path.Join(dirpath,filename), out tex)) throw new("bad digits");
        if(!SwSpriteAnimation.TryFromTexture(tex, new(digit_width,digit_height), out Digits)) throw new("poo2");
    }
    public void Draw()
    {
        Icons.Draw(Offset+IconOff, ItemIdx);
        Frame.Draw(Offset);
        if(Quantity > 9) Digits.Draw(Offset + TensOff, Quantity / 10);
        Digits.Draw(Offset + OnesOff, Quantity % 10);
    }
    public static bool TryLoad(ErVec2 offset, string dirpath, PriNode node, out SwHudItem hudItem)
    {
        hudItem = default!;
        try
        {
            hudItem = new(offset, dirpath, node);
            return true;
        }
        catch(Exception e)
        {
            return ErEngine.LogWarning(e);
        }
    }
}