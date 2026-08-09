using Eris;
using SpoonWitch.ByteStream;

namespace SpoonWitch.Rendering;

public readonly struct SwAnimationState
{
    public enum AnimDir: byte
    {
        Forward,
        Reversed,
    }
    public enum AnimFlags: byte
    {
        None = 0,
        IsPlaying = 1,
        IsDirNegative = 2,
        HFlip = 4,
        VFlip = 8,
        IsLooping = 16,
        IsReversed = 32,
        IsBouncing = 64,
    }
    public readonly AnimFlags Flags;
    public readonly int FrameIdx;
    public readonly double FrameProgress;
    public readonly double FrameDuration;
    public double Fps => 1/FrameDuration;
    public bool IsPlaying => HasFlags(AnimFlags.IsPlaying);
    public int Direction => HasFlags(AnimFlags.IsDirNegative) ? -1 : 1;
    public bool HFlip => HasFlags(AnimFlags.HFlip);
    public bool VFlip => HasFlags(AnimFlags.VFlip);
    public bool IsLooping => HasFlags(AnimFlags.IsLooping);
    public bool IsReversed => HasFlags(AnimFlags.IsReversed);
    public bool IsBouncing => HasFlags(AnimFlags.IsBouncing);
    public SwAnimationState(){}
    private SwAnimationState(int frame, double frameProgress, double frameDuration)
    {
        FrameIdx = frame;
        FrameProgress = frameProgress;
        FrameDuration = frameDuration;
    }
    private SwAnimationState(int frame, double frameProgress, double frameDuration, AnimFlags flags): this(frame, frameProgress, frameDuration)
    {
        Flags = flags;
    }
    public bool HasFlags(AnimFlags flags)
    {
        return (Flags & flags) == flags;
    }
    private static void SetFlags(ref AnimFlags flags, AnimFlags mask, bool value)
    {
        flags &= ~mask;
        flags |= value ? ((AnimFlags)255 & mask): 0;
    }
    public void Copy(ref SwAnimationState animState)
    {
        animState = new(FrameIdx, FrameProgress, FrameDuration, Flags);
    }
    public static void Set(ref SwAnimationState animState, int? frameIdx = null, double? frameProgress = null, double? fps = null, 
        bool? isPlaying = null, int? dir = null,
        bool? hFlip = null, bool? vFlip = null,
        bool? isLooping = null) //, bool? isReversed = null, bool? isBouncing = null)
    {
        AnimFlags flags = animState.Flags;
        if(isPlaying is not null) SetFlags(ref flags, AnimFlags.IsPlaying, isPlaying.Value);
        if(dir is not null) SetFlags(ref flags, AnimFlags.IsDirNegative, dir.Value < 0);
        if(hFlip is not null) SetFlags(ref flags, AnimFlags.HFlip, hFlip.Value);
        if(vFlip is not null) SetFlags(ref flags, AnimFlags.VFlip, vFlip.Value);
        if(isLooping is not null) SetFlags(ref flags, AnimFlags.IsLooping, isLooping.Value);
        // if(isReversed is not null) SetFlags(ref flags, AnimFlags.IsReversed, isReversed.Value);
        // if(isBouncing is not null) SetFlags(ref flags, AnimFlags.IsBouncing, isBouncing.Value);
        animState = new(frameIdx ?? animState.FrameIdx, frameProgress ?? animState.FrameProgress, 1/(fps ?? animState.Fps), flags);
    }
    public static void Advance(ref SwAnimationState animState, double dt, int numFrames)
    {
        if(!animState.IsPlaying) return;
        bool isPlaying = animState.IsPlaying;
        double progress = animState.FrameProgress + dt;
        int frame = animState.FrameIdx;
        while(progress > animState.FrameDuration && isPlaying)
        {
            progress -= animState.FrameDuration;
            frame += animState.Direction;
            if(frame >= 0 && frame < numFrames) continue;
            // if (animState.IsBouncing)
            // {
            //     ErEngine.LogWarning("bouncing animations not yet supported");
            //     return;
            // }
            // if (animState.IsReversed)
            // {
            //     ErEngine.LogWarning("reversed animations not yet supported");
            //     return;
            // }
            if (animState.IsLooping)
            {
                // Todo: implement this, it should work I'm just trying to keep things simple
                // frame = animState.IsReversed ? numFrames - 1 : 0;
                frame = 0;
                continue;
            }
            frame -= animState.Direction;
            isPlaying = false;
        }
        Set(ref animState, frame, progress, isPlaying:isPlaying);
    }
    public static bool TryRead(SwByteStream bs, ref SwAnimationState state)
    {
        if(!bs.TryReadI32(out int frame)) return false;
        if(!bs.TryReadF64(out double frameProgress)) return false;
        if(!bs.TryReadF64(out double frameDuration)) return false;
        if(!bs.TryReadByte(out byte b)) return false;
        var flags = (AnimFlags)b;
        state = new(frame, frameProgress, frameDuration, flags);
        return true;
    }
    public readonly void Write(SwByteStream bs)
    {
        bs.WriteI32(FrameIdx);
        bs.WriteF64(FrameProgress);
        bs.WriteF64(FrameDuration);
        bs.WriteByte((byte)Flags);
    }
}