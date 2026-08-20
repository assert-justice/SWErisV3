namespace Eris.Renderer;

public class ErFontManager
{
    private readonly Dictionary<(string,float), ErFont> FontLookup = [];
    public bool TryAddFont(string filepath, float size, ErFont font)
    {
        if(FontLookup.TryAdd((filepath, size), font)) return true;
        ErEngine.LogWarning("attempted to load a font from an extant filepath '", filepath, "'");
        font.Cleanup();
        return false;
    }
    public bool TryGetFont(string filepath, float size, out ErFont font)
    {
        return FontLookup.TryGetValue((filepath,size), out font!);
    }
    public void Cleanup()
    {
        foreach (var item in FontLookup.Values)
        {
            item.Cleanup();
        }
        FontLookup.Clear();
    }
}