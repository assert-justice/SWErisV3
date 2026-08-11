using Eris;
using Eris.Renderer;
using SpoonWitch.Data;

namespace SpoonWitch.Rendering;

public class SwTextureStore
{
    private readonly List<ErTexture?> CachedTextures;
    public ErTexture DefaultTexture;
    public SwTextureStore(ErTexture defaultTexture)
    {
        DefaultTexture = defaultTexture;
        CachedTextures = new(SwData.PaletteCount + 1)
        {
            defaultTexture
        };
    }
    public ErTexture? Get(int paletteIdx)
    {
        if(paletteIdx == 0) return CachedTextures[0];
        if(paletteIdx > 0 && paletteIdx < CachedTextures.Count && CachedTextures[paletteIdx] is ErTexture tex) return tex;
        if(paletteIdx < 1 || paletteIdx > SwData.PaletteCount)
        {
            ErEngine.LogError("bad pallet idx ", paletteIdx);
            return null;
        }
        while(paletteIdx >= CachedTextures.Count) CachedTextures.Add(null);
        if(!SwData.TryGetPalletTexture(out var texture, DefaultTexture.Filepath!, paletteIdx)) return null;
        CachedTextures[paletteIdx] = texture;
        return texture;
    }
}