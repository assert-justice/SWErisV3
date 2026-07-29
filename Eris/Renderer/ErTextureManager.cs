using SDL3;

namespace Eris.Renderer;

public class ErTextureManager
{
    private class PalletEqualityComparer : IEqualityComparer<int[]>
    {
        public bool Equals(int[]? x, int[]? y)
        {
            if(x is null || y is null) return false;
            if (x.Length != y.Length)
            {
                return false;
            }
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }
            return true;
        }
        public int GetHashCode(int[] obj)
        {
            int result = 17;
            for (int i = 0; i < obj.Length; i++)
            {
                unchecked
                {
                    result = result * 23 + obj[i];
                }
            }
            return result;
        }
    }
    private readonly nint RendererHandle;
    private readonly Dictionary<string, nint> TextureLookup = [];
    private readonly Dictionary<string, Dictionary<nint, nint>> TexturePaletteLookup = [];
    private readonly List<nint> OrphanTextures = [];
    private readonly Dictionary<string, nint> SurfaceLookup = [];
    private readonly Dictionary<string, Dictionary<nint, nint>> SurfacePaletteLookup = [];
    private readonly Dictionary<int[], nint> PaletteLookup = new(new PalletEqualityComparer());
    private static readonly List<int> PalletKeyBuilder = [];
    public ErTextureManager(nint rendererHandle)
    {
        RendererHandle = rendererHandle;
    }
    public bool TryGetSurface(string filepath, out nint surfaceHandle)
    {
        if(SurfaceLookup.TryGetValue(filepath, out surfaceHandle)) return true;
        surfaceHandle = SDL.LoadSurface(filepath);
        if(surfaceHandle == 0) return false;
        SurfaceLookup.Add(filepath, surfaceHandle);
        return true;
    }
    public bool TryGetSurfaceWithPalette(string filepath, nint paletteHandle, out nint surfaceHandle)
    {
        if(!SurfacePaletteLookup.TryGetValue(filepath, out var lookup))
        {
            lookup = [];
            SurfacePaletteLookup.Add(filepath, lookup);
        }
        if(lookup.TryGetValue(paletteHandle, out surfaceHandle)) return true;
        if(!TryGetSurface(filepath, out nint baseSurfaceHandle)) return false;
        surfaceHandle = SDL.DuplicateSurface(baseSurfaceHandle);
        SDL.SetSurfacePalette(surfaceHandle, paletteHandle);
        if(surfaceHandle == 0) return false;
        lookup.Add(paletteHandle, surfaceHandle);
        return true;
    }
    public nint GetPalette(IList<ErColor> colors)
    {
        PalletKeyBuilder.Clear();
        foreach (var color in colors)
        {
            PalletKeyBuilder.Add(color.ToInt());
        }
        int[] key = [..PalletKeyBuilder];
        if(PaletteLookup.TryGetValue(key, out nint paletteHandle)) return paletteHandle;
        paletteHandle = SDL.CreatePalette(colors.Count);
        SDL.SetPaletteColors(paletteHandle, [..colors.Select(c => c.ToSdlColor())], 0, colors.Count);
        PaletteLookup.Add(key, paletteHandle);
        return paletteHandle;
    }
    public nint GetPalette(IList<SDL.Color> colors)
    {
        PalletKeyBuilder.Clear();
        foreach (var color in colors)
        {
            PalletKeyBuilder.Add(ErColor.SdlColorToInt(color));
        }
        int[] key = [..PalletKeyBuilder];
        if(PaletteLookup.TryGetValue(key, out nint paletteHandle)) return paletteHandle;
        paletteHandle = SDL.CreatePalette(colors.Count);
        SDL.SetPaletteColors(paletteHandle, [..colors], 0, colors.Count);
        PaletteLookup.Add(key, paletteHandle);
        return paletteHandle;
    }

    public bool TryGetPalettes(string filepath, out nint[] palettes)
    {
        palettes = [];
        if(!TryGetSurface(filepath, out nint surfaceHandle)) return false;
        SDL.Surface surface = (SDL.Surface)SDL.PointerToStructure<SDL.Surface>(surfaceHandle)!;
        List<nint> handles = new(surface.Height);
        List<SDL.Color> colors = new(surface.Width);
        for (int y = 0; y < surface.Height; y++)
        {
            colors.Clear();
            for (int x = 0; x < surface.Width; x++)
            {
                SDL.ReadSurfacePixel(surfaceHandle, x, y, out byte r, out byte g, out byte b, out byte a);
                colors.Add(new(){R=r, G=g, B=b, A=a});
            }
            handles.Add(GetPalette(colors));
        }
        palettes = [..handles];
        return true;
    }
    public void AddTextureHandle(nint handle)
    {
        OrphanTextures.Add(handle);
    }
    public void AddTextureHandle(nint handle, string filepath)
    {
        TextureLookup.Add(filepath, handle);
    }
    public bool TryGetTexture(string filepath, out nint textureHandle)
    {
        if(TextureLookup.TryGetValue(filepath, out textureHandle)) return true;
        textureHandle = Image.LoadTexture(RendererHandle, filepath);
        if(textureHandle == 0)
        {
            ErEngine.LogError($"failed to load a texture at filepath '{filepath}'");
            return false;
        }
        TextureLookup.Add(filepath, textureHandle);
        return true;
    }
    public bool TryGetTexture(string filepath, out nint textureHandle, out nint surfaceHandle)
    {
        textureHandle = default;
        surfaceHandle = default;
        if(!TryGetSurface(filepath, out surfaceHandle)) return false;
        if(TextureLookup.TryGetValue(filepath, out textureHandle)) return true;
        textureHandle = SDL.CreateTextureFromSurface(RendererHandle, surfaceHandle);
        if(textureHandle == 0)
        {
            ErEngine.LogError($"failed to load a texture at filepath '{filepath}'");
            ErEngine.LogError(SDL.GetError());
            return false;
        }
        TextureLookup.Add(filepath, textureHandle);
        return true;
    }
    public bool TryGetTextureWithPallet(string filepath, nint palletHandle, out nint textureHandle)
    {
        textureHandle = default;
        if(!TexturePaletteLookup.TryGetValue(filepath, out var lookup))
        {
            lookup = [];
            TexturePaletteLookup.Add(filepath, lookup);
        }
        if(lookup.TryGetValue(palletHandle, out textureHandle)) return true;
        if(!TryGetSurfaceWithPalette(filepath, palletHandle, out nint surfaceHandle)) return false;
        textureHandle = SDL.CreateTextureFromSurface(RendererHandle, surfaceHandle);
        if(textureHandle == 0) return false;
        lookup.Add(palletHandle, textureHandle);
        ErEngine.Log("here");
        return true;
    }
    public void Cleanup()
    {
        foreach (nint handle in TextureLookup.Values)
        {
            SDL.DestroyTexture(handle);
        }
        foreach (nint handle in OrphanTextures)
        {
            SDL.DestroyTexture(handle);
        }
        foreach (nint handle in SurfaceLookup.Values)
        {
            SDL.DestroySurface(handle);
        }
        foreach (var item in PaletteLookup.Values)
        {
            SDL.DestroyPalette(item);
        }
        foreach (var item in TexturePaletteLookup.Values)
        {
            foreach (var val in item.Values)
            {
                SDL.DestroyTexture(val);
            }
        }
        foreach (var item in SurfacePaletteLookup.Values)
        {
            foreach (var val in item.Values)
            {
                SDL.DestroySurface(val);
            }
        }
    }
}