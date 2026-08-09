using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game;
using SpoonWitch.Rendering;

namespace SpoonWitch.UI.Hud;

public class SwHudSprite
{
    private readonly SwFrame[] Frames;
    private readonly Queue<(double,int)> FrameQueue = [];
    private double Clock;
    private readonly ErVec2 Offset;
    public int FrameIdx;
    private SwHudSprite(ErVec2 offset, string dirpath, PriNode node)
    {
        Offset = offset;
        if(!node.TryGet("texture_filepath", out string filepath)) throw new("bad filepath");
        filepath = Path.Join(dirpath, filepath);
        if(!ErTexture.TryFromPath(filepath, out ErTexture tex)) throw new("bad tex");
        double width = node.TryGet("width", out double d) ? d : tex.Size.X;
        double height = node.TryGet("height", out d) ? d : tex.Size.Y;
        Frames = [..SwFrame.GetAllFrames(tex, new(width, height))];
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
        Frames[FrameIdx].Draw(Offset);
    }
    public static bool TryLoad(ErVec2 offset, string dirpath, PriNode node, out SwHudSprite hudSprite)
    {
        hudSprite = default!;
        try
        {
            hudSprite = new(offset, dirpath, node);
            return true;
        }
        catch(Exception e)
        {
            return ErEngine.LogWarning(e);
        }
    }
    public static bool TryLoadList(string dirpath, PriNode node, in List<SwHudSprite> sprites)
    {
        if(!node.TryGet("slots", out PriList list)) return ErEngine.LogWarning("no slots found");
        foreach (var item in list.Values)
        {
            double x = item.TryGet("x", out double d) ? d : 0;
            double y = item.TryGet("y", out d) ? d : 0;
            if(!TryLoad(new(x,y), dirpath, node, out var sprite)) return false;
            sprites.Add(sprite);
        }
        return true;
    }
}