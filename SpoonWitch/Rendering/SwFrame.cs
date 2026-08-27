using Eris.Renderer;
using ErisMath;
using SpoonWitch.Utils;

namespace SpoonWitch.Rendering;

public readonly struct SwFrame(SwTextureStore texture, ErRect2 sourceRect)
{
    public readonly SwTextureStore Texture = texture;
    public readonly ErRect2 SourceRect = sourceRect;
    public void Draw(ErVec2 position, ErVec2? origin = null, double angle = 0, bool hFlip = false, bool vFlip = false)
    {
        Texture.DefaultTexture.Draw(position, SourceRect.Size, SourceRect, origin, angle, hFlip, vFlip);
    }
    public void Draw(ErVec2 position, int paletteIdx, ErVec2? origin = null, double angle = 0, bool hFlip = false, bool vFlip = false)
    {
        Texture.Get(paletteIdx)?.Draw(position, SourceRect.Size, SourceRect, origin, angle, hFlip, vFlip);
    }
    public static IEnumerable<SwFrame> GetAllFrames(SwTextureStore texture, ErVec2 frameSize, ErVec2? tileOffset = null, ErVec2? tilePadding = null)
    {
        SwTileSplitter tiles = new(texture.DefaultTexture.Size, frameSize, tileOffset, tilePadding);
        foreach (var item in tiles.GetAllTiles())
        {
            yield return new(texture, item);
        }
    }
    public static bool TryGetFrames(out IEnumerable<SwFrame> frames, SwTextureStore texture, ErVec2 frameSize, int firstFrame, int lastFrame, ErVec2? tileOffset = null, ErVec2? tilePadding = null)
    {
        frames = default!;
        SwTileSplitter tiles = new(texture.DefaultTexture.Size, frameSize, tileOffset, tilePadding);
        if(!tiles.TryGetTiles(out var tileRects, Enumerable.Range(firstFrame, lastFrame - firstFrame + 1))) return false;
        frames = [..tileRects.Select(t => new SwFrame(texture, t))];
        return true;
    }
    public static bool TryGetRects(out IEnumerable<ErRect2> rects, SwTextureStore texture, ErVec2 frameSize, int firstFrame, int lastFrame, ErVec2? tileOffset = null, ErVec2? tilePadding = null)
    {
        SwTileSplitter tiles = new(texture.DefaultTexture.Size, frameSize, tileOffset, tilePadding);
        return tiles.TryGetTiles(out rects, Enumerable.Range(firstFrame, lastFrame - firstFrame + 1));
    }
}