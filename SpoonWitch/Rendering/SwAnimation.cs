using Eris;
using Eris.Renderer;
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
    private static readonly ErVec2 DefaultSize = new(64,64);
    public static bool TryFromPri(out SwAnimation animation, string name, string dirpath, PriNode spriteData)
    {
        animation = default;
        if(!spriteData.Get("animations").TryGet(name, out PriDict animData)) return ErEngine.LogWarning(name, " animation does not exist");
        var defaultSize = ErVec2.FromPrion(spriteData, "width", "height", DefaultSize);
        // if(!TryGetTexture(out var texture, spriteData, animData, name, dirpath)) return ErEngine.LogWarning(name, " animation could not ");
        if(!animData.TryGet("texture", out string textureFilepath)) return ErEngine.LogWarning(name, " anim is missing texture field");
        textureFilepath = Path.Join(dirpath, textureFilepath);
        if(!ErTexture.TryFromPath(textureFilepath, out var texture)) return ErEngine.LogWarning("could not read texture at filepath ", textureFilepath);
        if(!animData.TryGet("first_frame", out int first_frame)) return ErEngine.LogWarning(name, " anim is missing first frame");
        if(!animData.TryGet("last_frame", out int last_frame)) return ErEngine.LogWarning(name, " anim is missing first frame");
        if(!animData.TryGet("fps", out double fps)) fps = 8;
        if(!animData.TryGet("h_flip", out bool h_flip)) h_flip = false;
        if(!animData.TryGet("v_flip", out bool v_flip)) v_flip = false;
        if(!animData.TryGet("loops", out bool loops)) loops = false;
        if(!animData.TryGet("autoplay", out bool autoplay)) autoplay = false;
        if(!SwFrame.TryGetFrames(out var frames, new(texture), defaultSize, first_frame, last_frame)) return ErEngine.LogWarning(name, " anim failed to get frames");
        // SwFrame[] frames = [..SwFrame.GetFrames(texture, defaultSize, first_frame, last_frame)];
        SwAnimationState defaultState = new();
        SwAnimationState.Set(ref defaultState, fps: fps, isPlaying:autoplay, hFlip:h_flip, vFlip:v_flip, isLooping: loops);
        animation = new(name, [..frames], defaultSize, defaultState);
        return true;
    }
}