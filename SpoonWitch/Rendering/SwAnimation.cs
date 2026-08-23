using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Data;
using SpoonWitch.Utils;

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
    public static bool TryFromPriAse(ref List<SwAnimation> animations, string dirpath, PriNode spriteData)
    {
        if(!spriteData.TryGet("filepath", out string aseFilepath)) return ErEngine.LogWarning("ase animation missing filepath");
        aseFilepath = Path.Join(dirpath, aseFilepath);
        if(!SwData.TryLoadPrion(aseFilepath, out var aseData)) return ErEngine.LogWarning("cannot read ase animation file");
        string aseDirpath = Path.GetDirectoryName(aseFilepath)!;
        HashSet<string> blacklist = [];
        if(spriteData.TryGet("blacklist", out PriList bl))
        {
            foreach (var item in bl.Data)
            {
                if(!item.TryAs(out string aName)) return false;
                blacklist.Add(aName);
            }
        }
        if(!aseData.Get("meta").TryGet("image", out string textureFilepath)) return ErEngine.LogWarning("ase animation missing image filepath");
        if(!ErTexture.TryFromPath(Path.Join(aseDirpath, textureFilepath), out var texture)) return ErEngine.LogWarning("ase animation could not read texture");
        if(!aseData.Get("meta").TryGet("frameTags", out PriList frameTags)) return ErEngine.LogWarning("ase animation missing image filepath");
        if(!aseData.TryGet("frames", out PriList frames)) return ErEngine.LogWarning("ase animation missing frames");
        SwTextureStore textureStore = new(texture);
        SwFrame[] fs = new SwFrame[frames.Data.Count];
        for (int idx = 0; idx < fs.Length; idx++)
        {
            var frame = frames.Data[idx].Get("frame");
            if(!frame.TryGet("x", out double x)) return false;
            if(!frame.TryGet("y", out double y)) return false;
            if(!frame.TryGet("w", out double w)) return false;
            if(!frame.TryGet("h", out double h)) return false;
            if(!frames.Data[idx].TryGet("duration", out double duration)) return false;
            fs[idx] = new(textureStore, new(x,y,w,h), duration);
        }
        foreach (var item in frameTags.Data)
        {
            if(!item.TryGet("name", out string animName)) return false;
            if(blacklist.Contains(animName)) continue;
            if(!item.TryGet("from", out int first_frame)) return false;
            if(!item.TryGet("to", out int last_frame)) return false;
            bool loops = item.TryGet("repeats", out string _);
            bool hFlip = item.TryGet("data", out string tagData) && tagData == "fliph";
            SwAnimationState defaultState = new();
            SwAnimationState.Set(ref defaultState, fps: 8, hFlip:hFlip, isLooping: loops);
            SwAnimation animation = new(animName, [..fs[first_frame..(last_frame+1)]], DefaultSize, defaultState);
            animations.Add(animation);
        }
        return true;
    }
    public static bool TryFromPri(out SwAnimation animation, string name, string dirpath, PriNode spriteData)
    {
        animation = default;
        if(!spriteData.Get("animations").TryGet(name, out PriDict animData)) return ErEngine.LogWarning(name, " animation does not exist");
        var defaultSize = SwPrion.GetVec2(spriteData, "width", "height", DefaultSize);
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