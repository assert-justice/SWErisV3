using Eris.Renderer;
using ErisMath;
using SpoonWitch.Utils;

namespace SpoonWitch.Rendering;

public readonly struct SwFrame(ErTexture texture, ErRect2 sourceRect)
{
    public readonly ErTexture Texture = texture;
    public readonly ErRect2 SourceRect = sourceRect;
    public void Draw(ErVec2 position, ErVec2? origin = null, double angle = 0, bool hFlip = false, bool vFlip = false)
    {
        Texture.Draw(position, SourceRect.Size, SourceRect, origin, angle, hFlip, vFlip);
    }
    public static IEnumerable<SwFrame> GetAllFrames(ErTexture texture, ErVec2 frameSize, ErVec2? tileOffset = null, ErVec2? tilePadding = null)
    {
        SwTileSplitter tiles = new(texture.Size, frameSize, tileOffset, tilePadding);
        foreach (var item in tiles.GetAllTiles())
        {
            yield return new(texture, item);
        }
    }
    public static bool TryGetFrames(out IEnumerable<SwFrame> frames, ErTexture texture, ErVec2 frameSize, int firstFrame, int lastFrame, ErVec2? tileOffset = null, ErVec2? tilePadding = null)
    {
        frames = default!;
        SwTileSplitter tiles = new(texture.Size, frameSize, tileOffset, tilePadding);
        if(!tiles.TryGetTiles(out var tileRects, Enumerable.Range(firstFrame, lastFrame - firstFrame + 1))) return false;
        frames = [..tileRects.Select(t => new SwFrame(texture, t))];
        return true;
    }
}