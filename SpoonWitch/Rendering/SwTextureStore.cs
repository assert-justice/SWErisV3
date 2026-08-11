using Eris.Renderer;

namespace SpoonWitch.Rendering;

public class SwTextureStore
{
    private static readonly List<nint> PaletteHandles = new(256);
    private readonly string Filepath;
    private readonly List<ErTexture?> CachedTextures;
    private SwTextureStore(string filepath, ErTexture defaultTexture)
    {
        Filepath = filepath;
        CachedTextures = new(PaletteHandles.Count + 1)
        {
            defaultTexture
        };
    }
    public ErTexture? Get(int paletteIdx)
    {
        if(paletteIdx == 0) return CachedTextures[0];
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
    public static bool TryCreate(string filepath, out SwTextureStore textureStore)
    {
        textureStore = default!;
        if(!ErTexture.TryFromPath(filepath, out var texture)) return false;
        textureStore = new(filepath, texture);
        return true;
    }
}