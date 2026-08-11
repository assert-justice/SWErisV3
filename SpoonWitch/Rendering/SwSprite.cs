using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.ByteStream;
using SpoonWitch.Game;

namespace SpoonWitch.Rendering;

public class SwSprite(string name)
{
    private enum SwSpriteFlags: byte
    {
        None = 0,
        IsPaused = 1,
        IsVisible = 2,
        IsCentered = 4,
    }
    public readonly string Name = name;
    private readonly List<SwAnimation> Animations = [];
    private readonly Dictionary<string, int> AnimationLookup = [];
    private SwAnimationState NextAnimationState;
    // These need to be serialized
    private SwAnimationState AnimationState;
    private int CurrentAnimIdx;
    public int PalletIdx{get; private set;}
    public double Angle = 0;
    public ErVec2 Offset = ErVec2.Zero;
    private SwSpriteFlags Flags = SwSpriteFlags.IsVisible | SwSpriteFlags.IsCentered;
    // These are derived from the above fields
    public bool IsPaused
    {
        get => HasFlags(SwSpriteFlags.IsPaused);
        private set => SetFlags(SwSpriteFlags.IsPaused, value);
    }
    public bool Visible
    {
        get => HasFlags(SwSpriteFlags.IsVisible);
        set => SetFlags(SwSpriteFlags.IsVisible, value);
    }
    public bool Centered
    {
        get => HasFlags(SwSpriteFlags.IsCentered);
        set => SetFlags(SwSpriteFlags.IsCentered, value);
    }
    public SwAnimation CurrentAnimation => Animations[CurrentAnimIdx];
    public bool IsPlaying
    {
        get => !IsPaused && AnimationState.IsPlaying;
    }
    public int FrameIdx
    {
        get => AnimationState.FrameIdx;
        set => SwAnimationState.Set(ref AnimationState, frameIdx: value);
    }
    public double FrameProgress
    {
        get => AnimationState.FrameProgress;
        set => SwAnimationState.Set(ref AnimationState, frameProgress:value);
    }
    public bool IsLooping => AnimationState.IsLooping;
    public void AddAnimation(SwAnimation animation)
    {
        if(!AnimationLookup.TryAdd(animation.Name, Animations.Count))
        {
            ErEngine.LogWarning("sprite already has an animation named '", animation.Name, "'");
            return;
        }
        Animations.Add(animation);
        if(Animations.Count == 1) SetAnimation(animation.Name);
    }
    private void SetAnimation(string animName)
    {
        if(!AnimationLookup.TryGetValue(animName, out int animIdx))
        {
            ErEngine.LogWarning("sprite has no animation named '", animName, "'");
            return;
        }
        CurrentAnimIdx = animIdx;
        AnimationState = CurrentAnimation.DefaultState;
    }
    public void Play()
    {
        if(IsPaused) IsPaused = false;
        if(!IsPlaying) SwAnimationState.Set(ref AnimationState, isPlaying:true, frameIdx:0, frameProgress:0);
    }
    public void Play(string animName)
    {
        if(animName != CurrentAnimation.Name) SetAnimation(animName);
        Play();
    }
    public void Pause()
    {
        SetFlags(SwSpriteFlags.IsPaused, true);
    }
    public void Stop()
    {
        SwAnimationState.Set(ref AnimationState, isPlaying:false);
    }
    private bool HasFlags(SwSpriteFlags flags)
    {
        return (Flags & flags) == flags;
    }
    private void SetFlags(SwSpriteFlags mask, bool value)
    {
        // clear masked flags
        Flags &= ~mask;
        Flags |= value ? (SwSpriteFlags)255&mask : 0;
    }
    public void SetPallet(int palletIdx)
    {
        PalletIdx = palletIdx;
    }
    public void Update()
    {
        if(Animations.Count == 0) return;
        if(CurrentAnimIdx < 0 || CurrentAnimIdx >= Animations.Count)
        {
            ErEngine.LogWarning("bad anim idx: ", CurrentAnimIdx);
            return;
        }
        SwAnimationState.Advance(ref AnimationState, SwGame.DeltaTime, CurrentAnimation.NumFrames);
    }
    public void Draw(ErVec2 position)
    {
        if(!Visible) return;
        if(Animations.Count == 0) return;
        AnimationState.Copy(ref NextAnimationState);
        SwAnimationState.Advance(ref NextAnimationState, SwGame.FrameDuration, CurrentAnimation.NumFrames);
        if(!CurrentAnimation.TryGetFrame(out var frame, NextAnimationState.FrameIdx))
        {
            ErEngine.LogError("bad frame idx ", NextAnimationState.FrameIdx, " for anim ", CurrentAnimation.Name);
            return;
        }
        ErVec2 origin = Centered ? frame.SourceRect.Size * 0.5 : ErVec2.Zero;
        frame.Draw(position + Offset, PalletIdx, origin, Angle, NextAnimationState.HFlip, NextAnimationState.VFlip);
    }
    public bool TryRead(SwByteStream byteStream)
    {
        if(!SwAnimationState.TryRead(byteStream, ref AnimationState)) return ErEngine.LogError("bad anim state");
        if(!byteStream.TryReadI32(out CurrentAnimIdx)) return ErEngine.LogError("bad current anim idx");
        if(!byteStream.TryReadF64(out Angle)) return ErEngine.LogError("bad angle");
        if(!byteStream.TryReadVec2(out Offset)) return ErEngine.LogError("bad offset");
        if(!byteStream.TryReadByte(out byte b)) return ErEngine.LogError("bad sprite flags");
        if(!byteStream.TryReadI32(out int palletIdx)) return ErEngine.LogError("missing pallet idx");
        PalletIdx = palletIdx;
        Flags = (SwSpriteFlags)b;
        return true;
    }
    public void Write(SwByteStream byteStream)
    {
        AnimationState.Write(byteStream);
        byteStream.WriteI32(CurrentAnimIdx);
        byteStream.WriteF64(Angle);
        byteStream.WriteVec2(Offset);
        byteStream.WriteByte((byte)Flags);
        byteStream.WriteI32(PalletIdx);
    }
    // private static bool TryGetPallet(out nint palletHandle, PriNode priNode)
    // {
    //     palletHandle = 0;
    //     return true;
    // }
    public static bool TryFromData(out SwSprite sprite, string name, string dirpath, PriNode priNode)
    {
        sprite = default!;
        if(!priNode.TryGet("animations", out PriDict dict)) return ErEngine.LogWarning("no animations");
        if(!priNode.TryGet("visible", out bool visible)) visible = true;
        // var size = ErVec2.FromPrion(priNode, "width", "height", new(64,64));
        var offset = ErVec2.FromPrion(priNode, "offset_x", "offset_y");
        if(!priNode.TryGet("centered", out bool centered)) centered = true;
        sprite = new(name)
        {
            Visible = visible,
            Offset = offset,
            Centered = centered,
        };
        foreach (var animName in dict.Data.Keys)
        {
            if(!SwAnimation.TryFromPri(out var animation, animName, dirpath, priNode)) ErEngine.LogWarning("bad animation '", animName, "'");
            else sprite.AddAnimation(animation);
        }
        return true;
    }
}