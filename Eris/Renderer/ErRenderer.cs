using ErisMath;
using SDL3;

namespace Eris.Renderer;

public class ErRenderer
{
    public ErTextureManager TextureManager{get; private set;} = null!;
    public readonly ErFontManager FontManager = new();
    private nint Window;
    private ErColor ClearColor = ErColor.Black;
    public string WindowName{get; private set;} = "Eris Engine";
    public ErVec2I WindowSize{get; private set;} = new(800, 600);
    public SDL.WindowFlags WindowFlags{get; private set;}
    public bool IsFullscreen{get; private set;}
    public nint Handle{get; private set;}
    public ErRect2 ViewportTransform{get; private set;}
    private readonly Stack<(ErRect2,nint)> ViewportStack = [];
    public void PushViewport(ErVec2 position, ErTexture target)
    {
        ViewportStack.Push((new(position,target.Size),target.Handle));
        UseViewport();
    }
    public void PopViewport()
    {
        if(!ViewportStack.TryPop(out _)) ErEngine.LogWarning("attempted to pop from empty viewport stack");
        else UseViewport();
    }
    private void UseViewport()
    {
        if(ViewportStack.TryPeek(out var result))
        {
            ViewportTransform = result.Item1;
            SDL.SetRenderTarget(Handle, result.Item2);
        }
        else
        {
            ViewportTransform = new(ErVec2.Zero, (ErVec2)WindowSize);
            SDL.SetRenderTarget(Handle, 0);
        }
    }
    private void ResetViewport()
    {
        ViewportTransform = new(ErVec2.Zero, ErVec2.One);
        SDL.SetRenderTarget(Handle, 0);
        ViewportStack.Clear();
    }
    private readonly Stack<Action> CleanupStack = new();
    public void Init()
    {
        if (!SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Gamepad))
        {
            ErEngine.LogError($"SDL could not initialize: {SDL.GetError()}");
            return;
        }
        CleanupStack.Push(SDL.Quit);
        if (!SDL.CreateWindowAndRenderer(WindowName, WindowSize.X, WindowSize.Y, 0, out Window, out var renderer))
        {
            ErEngine.LogError($"Error creating window and rendering: {SDL.GetError()}");
            return;
        }
        CleanupStack.Push(()=>SDL.DestroyWindow(Window));
        CleanupStack.Push(()=>SDL.DestroyRenderer(Handle));
        if (!TTF.Init())
        {
            ErEngine.LogError($"Error initializing font renderer: {SDL.GetError()}");
            return;
        }
        CleanupStack.Push(TTF.Quit);
        CleanupStack.Push(FontManager.Cleanup);
        Handle = renderer;
        SDL.SetRenderVSync(Handle, 1);
        SDL.SetDefaultTextureScaleMode(Handle, SDL.ScaleMode.Nearest);
        ResetViewport();
        TextureManager = new(Handle);
        CleanupStack.Push(TextureManager.Cleanup);
    }
    public void Cleanup()
    {
        while(CleanupStack.TryPop(out Action? result)) result();
    }
    public void SetWindow(string? name = null, ErVec2I? size = null, SDL.WindowFlags windowFlags = 0)
    {
        if(Window == 0)
        {
            if(name is not null) WindowName = name;
            if(size is not null) WindowSize = size.Value;
            WindowFlags = windowFlags;
            return;
        }
        ErEngine.LogError("Setting the size of the window is not yet implemented");
    }
    public void BeginRender()
    {
        Clear();
    }
    public void EndRender()
    {
        SDL.RenderPresent(Handle);
    }
    public void Clear()
    {
        SDL.SetRenderDrawColor(Handle, ClearColor.R, ClearColor.G, ClearColor.B, ClearColor.A);
        SDL.RenderClear(Handle);
    }
    public void SetClearColor(ErColor color)
    {
        ClearColor = color;
    }
    public void DebugDrawRect(ErColor color, ErRect2 rect, bool filled)
    {
        rect = rect.Translate(-ViewportTransform.Position);
        SDL.SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);
        if (filled)SDL.RenderFillRect(Handle, rect.ToSdlRect());
        else SDL.RenderRect(Handle, rect.ToSdlRect());
    }
    public void DebugDrawLine(ErColor color, ErVec2 start, ErVec2 end)
    {
        start -= ViewportTransform.Position;
        end -= ViewportTransform.Position;
        SDL.SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);
        SDL.RenderLine(Handle, (float)start.X, (float)start.Y, (float)end.X, (float)end.Y);
    }
}