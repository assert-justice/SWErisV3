using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Data;
using SpoonWitch.Game;
using SpoonWitch.Utils;

namespace SpoonWitch.UI.Hud;

public class SwHudBar
{
    private readonly ErVec2 Offset;
    private readonly ErTexture Cap;
    private readonly ErVec2 CapOff;
    private readonly ErTexture Fill;
    private readonly ErVec2 FillOff;
    private readonly ErTexture Seg;
    private readonly ErVec2 SegOff;
    private readonly double SegLen;
    private readonly ErTexture Bg;
    public double MaxValue = 100;
    private double _Value = 100;
    public double Value
    {
        get => _Value;
        set
        {
            value = Math.Clamp(value, 0, MaxValue);
            if(value < _Value) BgValue = _Value;
            _Value = value;
        }
    }
    private double BgValue = 100;
    public double BgUpdateSpeed = 50;
    public double HScale = 1;
    private SwHudBar(ErVec2 offset, string name, PriNode node)
    {
        var common = node.Get("bar_common");
        var data = node.Get("bars").Get(name);
        if(!SwData.TryLoadTexture(out Fill, data.Get("fill_filepath"))) throw new("bad fill_filepath");
        if(!SwData.TryLoadTexture(out Cap, data.Get("cap_filepath"))) throw new("bad cap_filepath");
        if(!SwData.TryLoadTexture(out Seg, data.Get("segment_filepath"))) throw new("bad segment_filepath");
        if(!SwData.TryLoadTexture(out Bg, data.Get("bg_filepath"))) throw new("bad bg_filepath");
        Offset = SwPrion.GetVec2(data) + offset;
        FillOff = SwPrion.GetVec2(common, "fill_x", "fill_y");
        CapOff = SwPrion.GetVec2(common, "cap_x", "cap_y");
        SegOff = SwPrion.GetVec2(common, "segment_x", "segment_y");
        SegLen = common.TryGet("segment_length_offset", out double d) ? d : 0;
        if(!data.Get("x").TryAs(out double ox)) ox = 0;
        if(!data.Get("y").TryAs(out double oy)) oy = 0;
        Offset = new ErVec2(ox, oy) + offset;
        if(!common.Get("fill_x").TryAs(out double fill_x)) fill_x = 0;
        if(!common.Get("fill_y").TryAs(out double fill_y)) fill_y = 0;
        FillOff = new(fill_x, fill_y);
        if(!common.Get("cap_x").TryAs(out double cap_x)) cap_x = 0;
        if(!common.Get("cap_y").TryAs(out double cap_y)) cap_y = 0;
        CapOff = new(cap_x, cap_y);
        if(!common.Get("segment_x").TryAs(out double segment_x)) segment_x = 0;
        if(!common.Get("segment_y").TryAs(out double segment_y)) segment_y = 0;
        SegOff = new(segment_x, segment_y);
        if(!common.Get("segment_length_offset").TryAs(out SegLen)) SegLen = 0;
    }
    public void Update()
    {
        if(BgValue > Value) BgValue -= BgUpdateSpeed * SwGame.DeltaTime;
    }
    public void Draw()
    {
        double length = Value * HScale;
        double maxLength = MaxValue * HScale + SegLen;
        ErVec2 capPos = Offset + CapOff + new ErVec2(maxLength, 0);
        if(BgValue > Value) Bg.Draw(Offset + FillOff, new(BgValue * HScale, Fill.Size.Y));
        Fill.Draw(Offset + FillOff, new(length, Fill.Size.Y));
        Seg.Draw(Offset + SegOff, new(maxLength, Seg.Size.Y));
        Cap.Draw(capPos);
    }
    public static bool TryLoad(out SwHudBar hudBar, ErVec2 offset, string name, PriNode node)
    {
        hudBar = default!;
        try
        {
            hudBar = new(offset, name, node);
            return true;
        }
        catch(Exception e)
        {
            return ErEngine.LogWarning(e);
        }
    }
}
