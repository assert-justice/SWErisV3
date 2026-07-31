using Eris;
using Eris.Renderer;
using ErisMath;

namespace SpoonWitch.Game.Entity.Component.Sprite;

public readonly struct SwFrame
{
    public ErTexture Texture{get; init;}
    public ErRect2 SourceRect{get; init;}
}

public class SwSpriteAnimation
{
    private class SwAnimState
    {
        public int Frame;
        public int FrameDir = 1;
        public double FrameProgress;
        public bool IsPlaying;
        public SwAnimState Clone(ref SwAnimState state)
        {
            state.Frame = Frame;
            state.FrameDir = FrameDir;
            state.FrameProgress = FrameProgress;
            state.IsPlaying = IsPlaying;
            return state;
        }
    }
    private SwAnimState State = new();
    private SwAnimState NextState = new();    
    public readonly string Name;
    public readonly List<SwFrame> Frames;
    public double Fps{get => 1/FrameDuration; set => FrameDuration = 1/value;}
    public double FrameDuration = 1/8.0;
    public bool IsReversed;
    public bool IsLooping;
    public bool IsBouncing;
    public int Frame
    {
        get => State.Frame;
        set
        {
            if(IsFrame(value)) State.Frame = value;
            else
            {
                ErEngine.LogError("Invalid frame ", value, " for animation '", Name, "'.");
                Stop();
            }
        }
    }
    public double FrameProgress
    {
        get => State.FrameProgress;
        set
        {
            State.FrameProgress = value;
        }
    }
    public int FrameDir
    {
        get => State.FrameDir;
        set
        {
            State.FrameDir = value;
        }
    }
    public readonly bool HFlip = false;
    public readonly bool VFlip = false;
    public bool IsPlaying{get => State.IsPlaying; set => State.IsPlaying = value;}
    private int FirstFrame{get => IsReversed ? Frames.Count - 1 : 0;}
    private int LastFrame{get => IsReversed ? 0 : Frames.Count - 1;}
    public SwSpriteAnimation(string name, IEnumerable<SwFrame> frames, bool hFlip = false, bool vFlip = false)
    {
        Name = name;
        Frames = [..frames];
        HFlip = hFlip;
        VFlip = vFlip;
    }
    public bool IsFrame(int frame)
    {
        return frame >= 0 && frame < Frames.Count;
    }
    public void Play()
    {
        State.IsPlaying = true;
    }
    public void Pause()
    {
        State.IsPlaying = false;
    }
    public void Stop()
    {
        State.Frame = FirstFrame;
        State.IsPlaying = false;
    }
    private void Next(double dt, ref SwAnimState animState)
    {
        if(!animState.IsPlaying) return;
        animState.FrameProgress += dt;
        while(animState.FrameProgress > FrameDuration)
        {
            animState.FrameProgress -= FrameDuration;
            int frame = animState.Frame + animState.FrameDir;
            if (IsFrame(frame))
            {
                animState.Frame = frame;
            }
            else if (IsBouncing)
            {
                if(frame == LastFrame)
                {
                    animState.FrameDir = -animState.FrameDir;
                    animState.Frame += animState.FrameDir;
                }
                else
                {
                    if(!IsLooping) animState.IsPlaying = false;
                    else
                    {
                        animState.FrameDir = -animState.FrameDir;
                        animState.Frame += animState.FrameDir;
                    }
                }
            }
            else if (IsLooping)
            {
                animState.Frame = FirstFrame;
            }
            else animState.IsPlaying = false;
        }
    }
    public void Update(double dt)
    {
        if(!IsPlaying) return;
        if(FrameProgress < FrameDuration)
        {
            Next(dt, ref State);
            if(!IsPlaying) Stop();
        }
    }
    public void Draw(SwSprite sprite, ErVec2 position, double dt)
    {
        // Todo: it looks like I'm not actually using hflip or vflip?
        if(!IsFrame(Frame)) return;
        State.Clone(ref NextState);
        Next(dt, ref NextState);
        var frame = Frames[NextState.Frame];
        ErVec2 origin = sprite.Centered ? frame.SourceRect.Size * 0.5 : ErVec2.Zero;
        bool hFlip = HFlip ? !sprite.HFlip : sprite.HFlip;
        bool vFlip = VFlip ? !sprite.VFlip : sprite.VFlip;
        frame.Texture.Draw(position + sprite.Offset, frame.SourceRect.Size, frame.SourceRect, origin, sprite.Angle, hFlip, vFlip);
    }
    public void Draw(ErVec2 position, int frameIdx)
    {
        if(!IsFrame(frameIdx)) return;
        var frame = Frames[frameIdx];
        frame.Texture.Draw(position, frame.SourceRect.Size, frame.SourceRect);
    }
    public static bool TryGetFrame(out SwFrame frame, ErTexture texture, int index, ErVec2 size)
    {
        frame = default;
        int xTiles = ErMath.FloorToInt(texture.Size.X / size.X);
        int yTiles = ErMath.FloorToInt(texture.Size.Y / size.Y);
        int yt = index / xTiles;
        int xt = index % xTiles;
        if(yt >= yTiles) return false;
        if(xt >= xTiles) return false;
        frame = new()
        {
            Texture = texture,
            SourceRect = new(size.X * xt, size.Y * yt, size.X, size.Y),
        };
        return true;
    }
    public static bool TryFromTexture(ErTexture texture, ErVec2 size, out SwSpriteAnimation spriteAnimation)
    {
        spriteAnimation = default!;
        int xTiles = ErMath.FloorToInt(texture.Size.X / size.X);
        int yTiles = ErMath.FloorToInt(texture.Size.Y / size.Y);
        List<SwFrame> frames = new(xTiles * yTiles);
        for (int xt = 0; xt < xTiles; xt++)
        {
            for (int yt = 0; yt < yTiles; yt++)
            {
                frames.Add(new(){Texture=texture,SourceRect=new(size.X * xt, size.Y * yt, size.X, size.Y)});
            }
        }
        spriteAnimation = new(string.Empty, frames);
        return true;
    }
}