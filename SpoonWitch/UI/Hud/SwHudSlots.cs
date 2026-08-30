using Eris;
using Prion.Node;
using SpoonWitch.Game;

namespace SpoonWitch.UI.Hud;

public class SwHudSlots
{
    private readonly List<SwHudSprite> Sprites = [];
    private readonly double FrameDuration = 1/16.0;
    private double FrameProgress = 0;
    private int FrameIdx;
    private int TargetFrameIdx;
    private const int NUM_FRAMES = 3;
    private int _Value;
    public int Value
    {
        get => _Value;
        set
        {
            FrameProgress = 0;
            TargetFrameIdx = value * NUM_FRAMES;
            _Value = value;
        }
    }
    public int MaxValue;
    public void Update()
    {
        if(FrameIdx == TargetFrameIdx) return;
        // calculate what frame we're on
        if(FrameIdx > TargetFrameIdx) FrameIdx = TargetFrameIdx;
        else
        {
            FrameProgress += SwGame.DeltaTime;
            while(FrameIdx < TargetFrameIdx && FrameProgress > FrameDuration)
            {
                FrameProgress -= FrameDuration;
                FrameIdx++;
            }
        }
        // apply that frame to the slot sprites
        int tempIdx = FrameIdx / NUM_FRAMES;
        for (int idx = 0; idx < Sprites.Count; idx++)
        {
            if(idx < tempIdx) Sprites[idx].FrameIdx = 0;
            else if(idx > tempIdx) Sprites[idx].FrameIdx = 2;
            else Sprites[idx].FrameIdx = 2 - (FrameIdx % NUM_FRAMES);
        }
    }
    public void Draw()
    {
        foreach (var item in Sprites)
        {
            item.Draw();
        }
    }
    public static bool TryLoad(out SwHudSlots slots, string dirpath, PriNode priNode)
    {
        slots = new();
        if(!SwHudSprite.TryLoadList(dirpath, priNode, slots.Sprites)) return false;
        slots.MaxValue = slots.Sprites.Count;
        slots.Value = slots.MaxValue;
        return true;
    }
}