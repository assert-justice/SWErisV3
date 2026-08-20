using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;

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
    public double MaxValue = 100;
    public double Value = 100;
    public double HScale = 1;
    private SwHudBar(ErVec2 offset, string dirpath, string name, PriNode node)
    {
        if(!node.Get("bar_common").TryAs(out PriNode common)) throw new("bad common bar");
        if(!node.Get("bars").Get(name).TryAs(out PriNode data)) throw new("bad bars");
        if(!data.Get("fill_filepath").TryAs(out string fill_filename)) throw new("bad fill_filepath");
        if(!ErTexture.TryFromPath(Path.Join(dirpath, fill_filename), out Fill)) throw new("bad fill_filepath");
        if(!data.Get("cap_filepath").TryAs(out string cap_filename)) throw new("bad cap_filepath");
        if(!ErTexture.TryFromPath(Path.Join(dirpath, cap_filename), out Cap)) throw new("bad cap_filepath");
        if(!data.Get("segment_filepath").TryAs(out string segment_filename)) throw new("bad segment_filepath");
        if(!ErTexture.TryFromPath(Path.Join(dirpath, segment_filename), out Seg)) throw new("bad segment_filepath");
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
    public void Update(){}
    public void Draw()
    {
        double length = Value * HScale;
        double maxLength = MaxValue * HScale + SegLen;
        ErVec2 capPos = Offset + CapOff + new ErVec2(maxLength, 0);
        Fill.Draw(Offset + FillOff, new(length, Fill.Size.Y));
        Seg.Draw(Offset + SegOff, new(maxLength, Seg.Size.Y));
        Cap.Draw(capPos);
    }
    public static bool TryLoad(ErVec2 offset, string dirpath, string name, PriNode node, out SwHudBar hudBar)
    {
        hudBar = default!;
        try
        {
            hudBar = new(offset, dirpath, name, node);
            return true;
        }
        catch(Exception e)
        {
            return ErEngine.LogWarning(e);
        }
    }
}