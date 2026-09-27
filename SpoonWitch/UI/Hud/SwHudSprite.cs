using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Data;
using SpoonWitch.Game;
using SpoonWitch.Rendering;
using SpoonWitch.Utils;

namespace SpoonWitch.UI.Hud;

public class SwHudSprite
{
    private readonly SwFrame[] Frames;
    private readonly Queue<(double,int)> FrameQueue = [];
    private double Clock;
    private readonly ErVec2 Offset;
    public int FrameIdx;
    public bool Visible = true;
    private SwHudSprite(ErVec2 offset, PriNode node)
    {
        Offset = offset;
        if(!SwData.TryLoadTexture(out var tex, node.Get("texture_filepath"))) throw new("bad texture");
        if(!SwPrion.TryGetVec2(out ErVec2 size, node, "width", "height")) size = tex.Size;
        Frames = [..SwFrame.GetAllFrames(new(tex), size)];
    }
    public void Update()
    {
        if(!FrameQueue.TryPeek(out var result)) return;
        if(Clock < result.Item1) Clock += SwGame.DeltaTime;
        else
        {
            Clock = 0;
            FrameIdx = result.Item2;
            FrameQueue.Dequeue();
        }
    }
    public void Draw()
    {
        if(!Visible) return;
        Frames[FrameIdx].Draw(Offset);
    }
    public static bool TryLoad(out SwHudSprite hudSprite, ErVec2 offset, PriNode node)
    {
        hudSprite = default!;
        try
        {
            hudSprite = new(offset, node);
            return true;
        }
        catch(Exception e)
        {
            return ErEngine.LogWarning(e);
        }
    }
    public static bool TryLoadList(in List<SwHudSprite> sprites, ErVec2 offset, PriNode node)
    {
        if(!node.TryGet("slots", out PriList list)) return ErEngine.LogWarning("no slots found");
        foreach (var item in list.Values)
        {
            var pos = SwPrion.GetVec2(item);
            if(!TryLoad(out var sprite, offset + pos, node)) return false;
            sprites.Add(sprite);
        }
        return true;
    }
}