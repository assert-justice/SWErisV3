using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Data;
using SpoonWitch.Utils;

namespace SpoonWitch.Rendering;

public class SwAseImporter
{
    private readonly Dictionary<string, PriNode> FrameTags = [];
    private readonly Dictionary<string,SwAnimation> Animations = [];
    private SwTextureStore TextureStore = null!;
    private List<PriNode> FrameList = null!;
    public ErVec2 FrameSize{get; private set;}
    public double Fps;
    private SwAseImporter(){}
    public SwAnimation? GetAnimation(string name)
    {
        if(Animations.TryGetValue(name, out var animation)) return animation;
        if(!FrameTags.TryGetValue(name, out var item)) return null;
        if(!item.TryGet("from", out int from)) return ErEngine.LogWarning("bad anim") ? null : null;
        if(!item.TryGet("to", out int to)) return ErEngine.LogWarning("bad anim") ? null : null;
        bool loops = !item.TryGet("repeat", out string _);
        bool hFlip = item.TryGet("data", out string _);
        SwFrame[] frames = new SwFrame[to - from + 1];
        for (int frameIdx = from; frameIdx <= to; frameIdx++)
        {
            var pos = SwPrion.GetVec2(FrameList[frameIdx].Get("frame"));
            frames[frameIdx - from] = new(TextureStore, new(pos, FrameSize));
        }
        SwAnimationState defaultState = new();
        SwAnimationState.Set(ref defaultState, hFlip:hFlip, isLooping: loops, fps:Fps);
        animation = new(name, frames, FrameSize, defaultState);
        Animations.Add(name, animation);
        return animation;
    }
    public bool TryGetAnimation(out SwAnimation animation, string name)
    {
        animation = default;
        if(GetAnimation(name) is not SwAnimation anim) return false;
        animation = anim;
        return true;
    }
    public IEnumerable<SwAnimation> GetAllAnimations()
    {
        foreach (var item in FrameTags.Keys)
        {
            if(TryGetAnimation(out var animation, item)) yield return animation;
        }
    }
    public IEnumerable<string> GetAnimationNames()
    {
        return FrameTags.Keys;
    }
    public static bool TryFromPriData(out SwAseImporter aseImporter, PriNode data)
    {
        aseImporter = new();
        var meta = data.Get("meta");
        if(!SwData.TryLoadTexture(out var texture, meta.Get("image"))) return ErEngine.LogWarning("ase bad texture");
        SwTextureStore textureStore = new(texture);
        aseImporter.TextureStore = textureStore;
        if(!data.TryGet("frames", out PriList frameList)) return ErEngine.LogWarning("ase bad frames");
        aseImporter.FrameList = frameList.Data;
        if(!meta.TryGet("frameTags", out PriList frameTags)) return ErEngine.LogWarning("ase bad frame tags");
        var firstFrame = frameList.Data[0];
        var frameSize = SwPrion.GetVec2(firstFrame.Get("frame"), "w", "h");
        aseImporter.FrameSize = frameSize;
        if(!firstFrame.TryGet("duration", out double duration)) duration = 125;
        aseImporter.Fps = 1000/duration;
        foreach (var item in meta.Get("frameTags").Values)
        {
            if(!item.TryGet("name", out string animName)) return ErEngine.LogWarning("bad anim");
            aseImporter.FrameTags.Add(animName, item);
        }
        return true;
    }
}