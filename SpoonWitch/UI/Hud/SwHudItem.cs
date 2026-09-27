using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Data;
using SpoonWitch.Rendering;
using SpoonWitch.Utils;

namespace SpoonWitch.UI.Hud;

public class SwHudItem
{
    private readonly ErVec2 Offset;
    private readonly ErTexture Frame;
    private readonly SwFrame[] Icons;
    private readonly ErVec2 IconOff;
    private readonly SwFrame[] Digits;
    private readonly ErVec2 TensOff;
    private readonly ErVec2 OnesOff;
    public int ItemIdx = 2;
    public int MaxQuantity;
    public int Quantity = 69;
    private SwHudItem(ErVec2 offset, PriNode node)
    {
        var icon = node.Get("item_slot");
        Offset = offset + SwPrion.GetVec2(icon);
        IconOff = SwPrion.GetVec2(icon, "icon_x", "icon_y");
        TensOff = SwPrion.GetVec2(icon, "tens_digit_x", "tens_digit_y");
        OnesOff = SwPrion.GetVec2(icon, "ones_digit_x", "ones_digit_y");
        var iconSize = SwPrion.GetVec2(icon, "icon_width", "icon_height");
        var digitSize = SwPrion.GetVec2(icon, "digit_width", "digit_height");
        if(!SwData.TryLoadTexture(out Frame, icon.Get("frame_filepath"))) throw new("bad frame");
        if(!SwData.TryLoadTexture(out var iconTex, icon.Get("icons_filepath"))) throw new("bad icons");
        if(!SwData.TryLoadTexture(out var digitsTex, icon.Get("digits_filepath"))) throw new("bad digits");
        Icons = [..SwFrame.GetAllFrames(new(iconTex), iconSize)];
        Digits = [..SwFrame.GetAllFrames(new(digitsTex), digitSize)];
    }
    public void Draw()
    {
        if(ItemIdx < 0 || ItemIdx >= Icons.Length)
        {
            ErEngine.LogError("bad icon idx ", ItemIdx);
            return;
        }
        Icons[ItemIdx].Draw(Offset+IconOff);
        Frame.Draw(Offset);
        if(Quantity > 9) Digits[Quantity / 10].Draw(Offset+TensOff);
        Digits[Quantity % 10].Draw(Offset+OnesOff);
    }
    public static bool TryLoad(out SwHudItem hudItem, ErVec2 offset, PriNode data)
    {
        hudItem = default!;
        try
        {
            hudItem = new(offset, data);
            return true;
        }
        catch(Exception e)
        {
            return ErEngine.LogWarning(e);
        }

    }
}
