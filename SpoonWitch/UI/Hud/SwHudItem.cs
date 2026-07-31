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
        if(!icon.Get("icon_x").TryAs(out double ix)) ix = 0;
        if(!icon.Get("icon_y").TryAs(out double iy)) iy = 0;
        IconOff = new(ix, iy);
        if(!icon.Get("tens_digit_x").TryAs(out double tx)) tx = 0;
        if(!icon.Get("tens_digit_y").TryAs(out double ty)) ty = 0;
        TensOff = new(tx, ty);
        if(!icon.Get("ones_digit_x").TryAs(out double ox)) ox = 0;
        if(!icon.Get("ones_digit_y").TryAs(out double oy)) oy = 0;
        OnesOff = new(ox, oy);
        if(!icon.Get("frame_filename").TryAs(out string filename)) throw new("bad frame file");
        if(!ErTexture.TryFromPath(Path.Join(dirpath,filename), out Frame)) throw new("bad frame");
        if(!icon.Get("icons_filename").TryAs(out filename)) throw new("bad icon file");
        if(!ErTexture.TryFromPath(Path.Join(dirpath,filename), out var tex)) throw new("bad icon");
        if(!SwSpriteAnimation.TryFromTexture(tex, new(32,32), out Icons)) throw new("poo");
        if(!icon.Get("digits_filename").TryAs(out filename)) throw new("bad digits file");
        if(!ErTexture.TryFromPath(Path.Join(dirpath,filename), out tex)) throw new("bad digits");
        if(!SwSpriteAnimation.TryFromTexture(tex, new(4,7), out Digits)) throw new("poo2");
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