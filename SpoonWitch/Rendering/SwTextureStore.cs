using Eris.Renderer;

namespace SpoonWitch.Rendering;

public class SwTextureStore(string filepath)
{
    private static readonly List<nint> PaletteHandles = new(256);
    private readonly string Filepath = filepath;
    private readonly List<ErTexture?> CachedTextures = [];
    public ErTexture? Get(int paletteIdx)
    {
        if(paletteIdx < 0) return null;
        if(paletteIdx >= PaletteHandles.Count) return null;
        while(paletteIdx >= CachedTextures.Count) CachedTextures.Add(null);
        if(CachedTextures[paletteIdx] is ErTexture tex) return tex;
        if(!ErTexture.TryFromPath(Filepath, PaletteHandles[paletteIdx], out tex)) return null;
        CachedTextures[paletteIdx] = tex;
        return tex;
    }
    public static void AddPallets(IEnumerable<nint> paletteHandles)
    {
        foreach (var item in paletteHandles)
        {
            PaletteHandles.Add(item);
        }
    }
}