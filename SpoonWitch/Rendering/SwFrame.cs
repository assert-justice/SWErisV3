using Eris;
using Eris.Renderer;
using ErisMath;

namespace SpoonWitch.Rendering;

public readonly struct SwFrame(ErTexture texture, ErRect2 sourceRect)
{
    public readonly ErTexture Texture = texture;
    public readonly ErRect2 SourceRect = sourceRect;
    private readonly struct SwTiles
    {
        public readonly ErTexture Texture;
        public readonly ErVec2 Offset;
        public readonly ErVec2 Padding;
        public readonly ErVec2 HalfPad;
        public readonly ErVec2 FullSize;
        public readonly ErVec2 FrameSize;
        public readonly ErVec2I GridSize;
        public readonly int NumFrames;
        public SwTiles(ErTexture texture, ErVec2 frameSize, ErVec2? tileOffset = null, ErVec2? tilePadding = null)
        {
            Texture = texture;
            Offset = tileOffset ?? ErVec2.Zero;
            Padding = tilePadding ?? ErVec2.Zero;
            HalfPad = Padding * 0.5;
            FullSize = frameSize + Padding;
            FrameSize = frameSize;
            GridSize = ((Texture.Size - Offset + Padding) / FullSize).FloorToInt();
            NumFrames = GridSize.GetArea();
        }
        public bool IsFrame(int frameIdx)
        {
            return frameIdx >= 0 && frameIdx < NumFrames;
        }
        public bool TryGetFrame(out SwFrame frame, int frameIdx)
        {
            frame = default;
            if(!IsFrame(frameIdx)) return false;
            ErVec2I frameCoords = new(frameIdx % GridSize.X, frameIdx / GridSize.X);
            ErVec2 pos = Offset + (ErVec2)frameCoords * FullSize - HalfPad;
            frame = new(Texture, new(pos, FrameSize));
            return true;
        }
        public IEnumerable<SwFrame> GetAllFrames()
        {
            for (int idx = 0; idx < NumFrames; idx++)
            {
                if(TryGetFrame(out var frame, idx)) yield return frame;
            }
        }
    }
    public void Draw(ErVec2 position, ErVec2? origin = null, double angle = 0, bool hFlip = false, bool vFlip = false)
    {
        Texture.Draw(position, SourceRect.Size, SourceRect, origin, angle, hFlip, vFlip);
    }
    public static IEnumerable<SwFrame> GetAllFrames(ErTexture texture, ErVec2 frameSize, ErVec2? tileOffset = null, ErVec2? tilePadding = null)
    {
        SwTiles tiles = new(texture, frameSize, tileOffset, tilePadding);
        return tiles.GetAllFrames();
    }
    public static IEnumerable<SwFrame> GetFrames(ErTexture texture, ErVec2 frameSize, IEnumerable<int> frameIds, ErVec2? tileOffset = null, ErVec2? tilePadding = null)
    {
        SwTiles tiles = new(texture, frameSize, tileOffset, tilePadding);
        foreach (var idx in frameIds)
        {
            if(tiles.TryGetFrame(out var frame, idx)) yield return frame;
            else ErEngine.LogWarning("bad frame idx ", idx);
        }
    }
    public static IEnumerable<SwFrame> GetFrames(ErTexture texture, ErVec2 frameSize, int firstFrame, int lastFrame, ErVec2? tileOffset = null, ErVec2? tilePadding = null)
    {
        SwTiles tiles = new(texture, frameSize, tileOffset, tilePadding);
        int numFrames = Math.Abs(lastFrame - firstFrame) + 1;
        if(firstFrame <= lastFrame)
        {
            for (int idx = 0; idx < numFrames; idx++)
            {
                if(!tiles.TryGetFrame(out var frame, firstFrame + idx))
                {
                    ErEngine.LogWarning("bad frame idx ", idx);
                    yield break;
                }
                yield return frame;
            }
        }
        else
        {
            for (int idx = 0; idx < numFrames; idx++)
            {
                if(!tiles.TryGetFrame(out var frame, lastFrame - 1 - idx))
                {
                    ErEngine.LogWarning("bad frame idx ", idx);
                    yield break;
                }
                yield return frame;
            }
        }
    }
}