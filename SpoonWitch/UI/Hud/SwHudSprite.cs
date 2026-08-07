using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game;
using SpoonWitch.Game.Entity.Component.Sprite;

namespace SpoonWitch.UI.Hud;

public class SwHudSprite
{
    private readonly SwSpriteAnimation Animation;
    private readonly Queue<(double,int)> FrameQueue = [];
    private double Clock;
    private readonly ErVec2 Offset;
    public int FrameIdx;
    public SwHudSprite(ErVec2 offset, string filepath)
    {
        Offset = offset;
        // string filepath = "game_data/hud/vitality_root_slot.png";
        if(!ErTexture.TryFromPath(filepath, out ErTexture tex)) throw new("bad tex");
        if(!SwSpriteAnimation.TryFromTexture(tex, new(tex.Size.X / 3, tex.Size.Y), out Animation)) throw new("bad anim");
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
        Animation.Draw(Offset, FrameIdx);
    }
    public static bool TryLoad(ErVec2 offset, string filepath, out SwHudSprite hudSprite)
    {
        hudSprite = default!;
        try
        {
            hudSprite = new(offset, filepath);
            return true;
        }
        catch(Exception e)
        {
            return ErEngine.LogWarning(e);
        }
    }
}