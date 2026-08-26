using ErisMath;
using SDL3;

namespace Eris.Renderer;

public class ErTexture
{
    public readonly nint Handle;
    public readonly ErVec2 Size;
    public readonly string? Filepath;
    private ErTexture(nint handle, string? filepath = null)
    {
        Handle = handle;
        Filepath = filepath;
        SDL.GetTextureSize(Handle, out float w, out float h);
        Size = new(w, h);
    }
    public static bool TryGetPaletteHandles(out nint[] palletHandles, string filepath)
    {
        return ErEngine.Renderer.TextureManager.TryGetPalettes(filepath, out palletHandles);
    }
    public static bool TryGetUnmanagedTexture(nint surfaceHandle, out ErTexture texture)
    {
        texture = default!;
        if(surfaceHandle == 0) return false;
        nint textureHandle = SDL.CreateTextureFromSurface(ErEngine.Renderer.Handle, surfaceHandle);
        if(textureHandle == 0) return false;
        texture = new(textureHandle);
        return true;
    }
    public static ErTexture GetRenderTexture(int width, int height)
    {
        if(ErEngine.Renderer is null) throw new Exception("Renderer not initialized");
        nint handle = SDL.CreateTexture(ErEngine.Renderer.Handle, SDL.PixelFormat.RGBA8888, SDL.TextureAccess.Target, width, height);
        ErEngine.Renderer.TextureManager.AddTextureHandle(handle);
        return new(handle);
    }
    public static ErTexture GetColoredTexture(int width, int height, ErColor color)
    {
        if(ErEngine.Renderer is null) throw new Exception("Renderer not initialized");
        nint surface = SDL.CreateSurface(width, height, SDL.PixelFormat.RGBA8888);
        SDL.Rect rect = new()
        {
            X = 0,
            Y = 0,
            W = width,
            H = height,
        };
        uint c = SDL.MapSurfaceRGBA(surface, color.R, color.G, color.B, color.A);
        SDL.FillSurfaceRect(surface, rect, c);
        nint handle = SDL.CreateTextureFromSurface(ErEngine.Renderer.Handle, surface);
        ErEngine.Renderer.TextureManager.AddTextureHandle(handle);
        SDL.DestroySurface(surface);
        return new(handle);
    }
    public static bool TryFromPath(string filepath, out ErTexture texture)
    {
        texture = default!;
        if(ErEngine.Renderer is null) return false;
        if(!ErEngine.Renderer.TextureManager.TryGetTexture(filepath, out nint handle)) return false;
        texture = new(handle, filepath);
        return true;
    }
    public static bool TryFromPath(string filepath, out ErTexture texture, out nint surfaceHandle)
    {
        texture = default!;
        surfaceHandle = default;
        if(ErEngine.Renderer is null) return false;
        if(!ErEngine.Renderer.TextureManager.TryGetTexture(filepath, out nint textureHandle, out surfaceHandle)) return false;
        texture = new(textureHandle, filepath);
        return true;
    }
    public static bool TryFromPath(string filepath, nint paletteHandle, out ErTexture texture)
    {
        texture = default!;
        if(ErEngine.Renderer is null) return false;
        if(!ErEngine.Renderer.TextureManager.TryGetTextureWithPalette(filepath, paletteHandle, out nint handle)) return false;
        texture = new(handle, filepath);
        return true;
    }
    public void Cleanup()
    {
        SDL.DestroyTexture(Handle);
    }
    public void Draw(ErVec2 position,
        ErVec2? size = null,
        ErRect2? sourceRect = null,
        ErVec2? origin = null,
        double angle = 0,
        bool hFlip = false,
        bool vFlip = false)
    {
        size ??= Size;
        ErRect2 destRect = new(position, size.Value);
        sourceRect ??= new(ErVec2.Zero, Size);
        origin ??= ErVec2.Zero;
        destRect = destRect.Translate(-ErEngine.Renderer.ViewportTransform.Position-origin.Value);
        SDL.FlipMode flipMode = SDL.FlipMode.None;
        if(hFlip) flipMode |= SDL.FlipMode.Horizontal;
        if(vFlip) flipMode |= SDL.FlipMode.Vertical;
        SDL.RenderTextureRotated(ErEngine.Renderer.Handle, 
            Handle, 
            sourceRect.Value.ToSdlRect(), 
            destRect.ToSdlRect(),
            ErMath.RadToDeg(angle),
            origin.Value.ToSdlPoint(),
            flipMode);
    }
    public void DrawFullscreen()
    {
        var viewSize = ErEngine.Renderer.ViewportTransform.Size;
        double xScale = viewSize.X / Size.X;
        double yScale = viewSize.Y / Size.Y;
        double scale = xScale < yScale ? xScale : yScale;
        ErVec2 size = Size * scale;
        ErRect2 destRect = new(viewSize * 0.5 - size * 0.5, size);
        ErRect2 srcRect = new(ErVec2.Zero,Size);
        SDL.RenderTexture(ErEngine.Renderer.Handle, Handle, srcRect.ToSdlRect(), destRect.ToSdlRect());
    }
    public void DrawQuick(ErVec2 position, ErVec2? size = null, ErVec2? srcPos = null, ErVec2? srcSize = null)
    {
        ErRect2 destRect = new(position - ErEngine.Renderer.ViewportTransform.Position, size ?? Size);
        ErRect2 srcRect = new(srcPos ?? ErVec2.Zero, srcSize ?? Size);
        SDL.RenderTexture(ErEngine.Renderer.Handle, Handle, srcRect.ToSdlRect(), destRect.ToSdlRect());
    }
}