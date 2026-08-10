using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Data;

namespace SpoonWitch.Rendering;

public readonly struct SwAnimation(string name, SwFrame[] frames, ErVec2 size, SwAnimationState defaultState)
{
    public readonly string Name = name;
    public readonly ErVec2 Size = size;
    public readonly SwAnimationState DefaultState = defaultState;
    public int NumFrames => Frames.Length;
    private readonly SwFrame[] Frames = frames;
    public bool IsFrame(int frameIdx)
    {
        return frameIdx >= 0 && frameIdx < NumFrames;
    }
    public bool TryGetFrame(out SwFrame frame, int frameIdx)
    {
        frame = default;
        if(!IsFrame(frameIdx)) return false;
        frame = Frames[frameIdx];
        return true;
    }
    public static bool TryFromPri(out SwAnimation animation, string name, string dirpath, ErVec2 size, PriNode priNode)
    {
        animation = default;
        if(!SwApp.TryGetTex(priNode, "texture", dirpath, out var texture)) return false;
        if(!priNode.TryGet("first_frame", out int first_frame)) return false;
        if(!priNode.TryGet("last_frame", out int last_frame)) return false;
        if(!priNode.TryGet("fps", out double fps)) fps = 8;
        if(!priNode.TryGet("h_flip", out bool h_flip)) h_flip = false;
        if(!priNode.TryGet("v_flip", out bool v_flip)) v_flip = false;
        if(!priNode.TryGet("loops", out bool loops)) loops = false;
        if(!priNode.TryGet("autoplay", out bool autoplay)) autoplay = false;
        SwFrame[] frames = [..SwFrame.GetFrames(texture, size, first_frame, last_frame)];
        SwAnimationState defaultState = new();
        SwAnimationState.Set(ref defaultState, fps: fps, isPlaying:autoplay, hFlip:h_flip, vFlip:v_flip, isLooping: loops);
        animation = new(name, frames, size, defaultState);
        return true;
    }
}