using ErisMath;
using SDL3;

namespace Eris.Renderer;
public class ErFont
{
    private readonly nint Handle;
    public readonly double FontSize;
    private readonly Dictionary<(uint, ErColor), ErTexture> GlyphCache = [];
    private ErFont(nint fontHandle, double fontSize)
    {
        Handle = fontHandle;
        FontSize = fontSize;
    }
    public void Cleanup()
    {
        foreach (var item in GlyphCache.Values)
        {
            item.Cleanup();
        }
        TTF.CloseFont(Handle);
    }
    public bool TryGetGlyph(uint ch, ErColor color, out ErTexture texture)
    {
        if(GlyphCache.TryGetValue((ch, color), out texture!)) return true;
        if(!TTF.FontHasGlyph(Handle, ch)) return false;
        nint surfaceHandle = TTF.RenderGlyphSolid(Handle, ch, color.ToSdlColor());
        if(!ErTexture.TryGetUnmanagedTexture(surfaceHandle, out texture)) return false;
        SDL.DestroySurface(surfaceHandle);
        GlyphCache.Add((ch, color), texture);
        return true;
    }
    public void DrawGlyph(uint ch, ErColor color, ErVec2 position)
    {
        if(!TryGetGlyph(ch, color, out var texture))
        {
            ErEngine.LogWarning("no glyph for character '", ch, "' found");
            return;
        }
        texture.Draw(position);
    }
    public void DrawString(string str, ErColor color, ErVec2 position)
    {
        // Todo: add unicode support
        // Todo: kerning
        double startX = position.X;
        double x = 0;
        double y = 0;
        foreach (char c in str)
        {
            if(c == '\n')
            {
                x = startX;
                y += FontSize;
                continue;
            }
            if(!TryGetGlyph(c, color, out var texture))
            {
                // Todo: handle this more gracefully
                ErEngine.LogError($"No char '{c}' exists in this font");
                continue;
            }
            texture.Draw(new(x,y));
            x += texture.Size.Y;
        }
    }
    public bool TryGetTexture(string text, ErColor color, out ErTexture texture)
    {
        nint surfaceHandle = TTF.RenderTextSolid(Handle, text, (nuint)text.Length, color.ToSdlColor());
        if(!ErTexture.TryGetUnmanagedTexture(surfaceHandle, out texture)) return ErEngine.LogWarning("could not create texture");
        SDL.DestroySurface(surfaceHandle);
        return true;
    }
    public static bool TryLoad(string filepath, float fontSize, out ErFont font)
    {
        if(ErEngine.Renderer.FontManager.TryGetFont(filepath, fontSize, out font)) return true;
        nint handle = TTF.OpenFont(filepath, fontSize);
        if(handle == 0) return false;
        font = new(handle, fontSize);
        return ErEngine.Renderer.FontManager.TryAddFont(filepath, fontSize, font);
    }
}