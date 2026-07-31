using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.ByteStream;

namespace SpoonWitch.Game.Entity.Component.Sprite;

public class SwSprite : SwComponent
{
    private readonly List<SwSpriteAnimation> Animations = [];
    private readonly Dictionary<string, int> AnimationLookup = [];
    private int CurrentAnimIdx;
    public SwSpriteAnimation? CurrentAnim
    {
        get
        {
            if(CurrentAnimIdx >= Animations.Count) return null;
            return Animations[CurrentAnimIdx];
        }
    }
    public bool HFlip = false;
    public bool VFlip = false;
    public int Frame{get => CurrentAnim?.Frame ?? 0;}
    public double FrameProgress{get => CurrentAnim?.FrameProgress ?? 0;}
    public bool IsPlaying{get => CurrentAnim?.IsPlaying ?? false;}
    public bool Visible = true;
    public bool Centered = true;
    public double Angle = 0;
    public ErVec2 Offset = ErVec2.Zero;
    public ErVec2 Size{get; private set;} = new(64,64);
    public SwSprite(SwEntity parent, string name) : base(parent, name)
    {
    }
    public void AddAnimation(SwSpriteAnimation animation)
    {
        // Note: We're not using the add method here animations can be replaced
        if(AnimationLookup.TryGetValue(animation.Name, out int idx))
        {
            Animations[idx] = animation;
        }
        else
        {
            idx = Animations.Count;
            Animations.Add(animation);
            AnimationLookup[animation.Name] = idx;
        }
        if(animation.IsPlaying) CurrentAnimIdx = idx;
    }
    public void Play()
    {
        CurrentAnim?.Play();
    }
    public void Play(string name)
    {
        if(CurrentAnim is not null && CurrentAnim.Name == name) CurrentAnim.Play();
        if(!AnimationLookup.TryGetValue(name, out var anim))
        {
            ErEngine.LogError("Unknown animation name '", name, "'.");
            return;
        }
        CurrentAnimIdx = anim;
        CurrentAnim?.Play();
    }
    public void Pause()
    {
        CurrentAnim?.Pause();
    }
    public void Stop()
    {
        CurrentAnim?.Stop();
    }
    public override void Read(SwByteStream byteStream)
    {
        byteStream.TryReadI32(out CurrentAnimIdx);
        byteStream.TryReadBool(out HFlip);
        byteStream.TryReadBool(out VFlip);
        byteStream.TryReadF64(out Angle);
        byteStream.TryReadI32(out int frame);
        byteStream.TryReadF64(out double frameProgress);
        byteStream.TryReadBool(out bool isPlaying);
        byteStream.TryReadBool(out bool frameDir);
        if(CurrentAnim is not null)
        {
            CurrentAnim.Frame = frame;
            CurrentAnim.FrameProgress = frameProgress;
            CurrentAnim.FrameDir = frameDir ? 1 : -1;
            CurrentAnim.IsPlaying = isPlaying;
        }
    }
    public override void Write(SwByteStream byteStream)
    {
        byteStream.WriteI32(CurrentAnimIdx);
        byteStream.WriteBool(HFlip);
        byteStream.WriteBool(VFlip);
        byteStream.WriteF64(Angle);
        byteStream.WriteI32(Frame);
        byteStream.WriteF64(FrameProgress);
        byteStream.WriteBool(IsPlaying);
        byteStream.WriteBool(CurrentAnim?.FrameDir > 0);
    }
    public override void Update()
    {
        base.Update();
        CurrentAnim?.Update(SwGame.FrameTime);
    }
    public override void Draw(SwComponent nextState)
    {
        if(!Visible) return;
        ErVec2 pos = ErMath.Lerp(Parent.Position, nextState.Parent.Position, SwGame.FrameProgress);
        CurrentAnim?.Draw(this, pos, SwGame.FrameTime);
    }
    public static bool TryFromData(string filepath, PriDict spriteData, SwEntity parent, string name, out SwSprite sprite)
    {
        sprite = new(parent, name);
        List<SwFrame> frames = [];
        string dirpath = Path.GetDirectoryName(filepath) ?? "./";
        if(!spriteData.Get("visible").TryAs(out bool visible)) visible = true;
        sprite.Visible = visible;
        if(!spriteData.Get("centered").TryAs(out bool centered)) centered = true;
        sprite.Centered = centered;
        if(!spriteData.Get("offset_x").TryAs(out int offsetX)) offsetX = 0;
        if(!spriteData.Get("offset_y").TryAs(out int offsetY)) offsetY = 0;
        sprite.Offset = new(offsetX, offsetY);
        if(!spriteData.Get("width").TryAs(out int width)) width = 64;
        if(!spriteData.Get("height").TryAs(out int height)) height = 64;
        sprite.Size = new(width, height);
        if(!spriteData.Get("animations").TryAs(out PriDict animations)) return false;
        foreach (var (animName, animData) in animations.Data)
        {
            if(!animData.Get("texture").TryAs(out string texturePath)) return false;
            texturePath = Path.Join(dirpath, texturePath);
            if(!ErTexture.TryFromPath(texturePath, out var texture)) return ErEngine.LogError("Invaid texture path '", texturePath, "'.");
            if(!animData.Get("first_frame").TryAs(out int firstFrame)) return false;
            if(!animData.Get("last_frame").TryAs(out int lastFrame)) return false;
            if(!animData.Get("fps").TryAs(out double fps)) fps = 8;
            if(!animData.Get("h_flip").TryAs(out bool hFlip)) hFlip = false;       
            if(!animData.Get("v_flip").TryAs(out bool vFlip)) vFlip = false;
            if(!animData.Get("autoplay").TryAs(out bool autoplay)) autoplay = false;
            if(!animData.Get("loops").TryAs(out bool loops)) loops = false;
            if(!animData.Get("bounce").TryAs(out bool bounce)) bounce = false;
            if(!animData.Get("reversed").TryAs(out bool reversed)) reversed = false;
            frames.Clear();
            foreach (var frameIdx in ErMath.Range(firstFrame, lastFrame))
            {
                if(SwSpriteAnimation.TryGetFrame(out var frame, texture, frameIdx, sprite.Size)) frames.Add(frame);
                else return ErEngine.LogError("Invalid frame index '", frameIdx, "' for texture '", texturePath, "'.");
            }
            SwSpriteAnimation anim = new(animName, frames, hFlip, vFlip)
            {
                Fps = fps,
                IsLooping = loops,
                IsBouncing = bounce,
                IsReversed = reversed,
            };
            if(autoplay)anim.Play();
            sprite.AddAnimation(anim);
        }
        return true;
    }
}