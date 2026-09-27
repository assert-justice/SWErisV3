using Eris;
using ErisMath;
using SDL3;
using SpoonWitch.Game.Map.MapData;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Map.MapDisplay;

public enum SwTileMask: byte
{
    None = 0,
    TopLeft = 1,
    TopRight = 2,
    BottomLeft = 4,
    BottomRight = 8,
}

public class SwMapDisplay
{
    // public readonly ErVec2I TileSize;
    // public readonly SwTileData2[] TileData;
    SwMap Map;
    private readonly SwTileMask[] TileMasks;
    private const int ATLAS_WIDTH = 4;
    private const int ATLAS_HEIGHT = 4;
    private readonly List<Dictionary<ErVec2I,(int tileId, int firstFrameIdx, int lastFrameIdx)>> DisplayLayers = [];
    // tileId, mask, variant
    private readonly (int firstFrameIdx, int lastFrameIdx)[][][] Animations = [];
    private readonly List<SwFrame> Frames = [];
    public SwMapDisplay(SwMap map)
    {
        Map = map;
        // TileSize = map.TileSize;
        // TileData = map.TileData;
        TileMasks = new SwTileMask[ATLAS_WIDTH * ATLAS_HEIGHT];
        AddTilemask(2,1, SwTileMask.TopLeft | SwTileMask.TopRight | SwTileMask.BottomLeft | SwTileMask.BottomRight); // All corners
        AddTilemask(1,3, SwTileMask.BottomRight); // Outer bottom-right corner
        AddTilemask(0,0, SwTileMask.BottomLeft); // Outer bottom-left corner
        AddTilemask(0,2, SwTileMask.TopRight); // Outer top-right corner
        AddTilemask(3,3, SwTileMask.TopLeft); // Outer top-left corner
        AddTilemask(1,0, SwTileMask.TopRight | SwTileMask.BottomRight); // Right edge
        AddTilemask(3,2, SwTileMask.TopLeft | SwTileMask.BottomLeft); // Left edge
        AddTilemask(3,0, SwTileMask.BottomLeft | SwTileMask.BottomRight); // Bottom edge
        AddTilemask(1,2, SwTileMask.TopLeft | SwTileMask.TopRight); // Top edge
        AddTilemask(1,1, SwTileMask.TopRight | SwTileMask.BottomLeft | SwTileMask.BottomRight); // Inner bottom-right corner
        AddTilemask(2,0, SwTileMask.TopLeft | SwTileMask.BottomLeft | SwTileMask.BottomRight); // Inner top-left corner
        AddTilemask(2,2, SwTileMask.TopLeft | SwTileMask.TopRight | SwTileMask.BottomRight); // Inner top-right corner
        AddTilemask(3,1, SwTileMask.TopLeft | SwTileMask.TopRight | SwTileMask.BottomLeft); // Inner top-left corner
        AddTilemask(2,3, SwTileMask.TopRight | SwTileMask.BottomLeft); // Bottom-left top-right corners
        AddTilemask(0,1, SwTileMask.TopLeft | SwTileMask.BottomRight); // Top-left down-right corners
        // generate tile frames/animations
    }
    public void Draw()
    {
        // draw layers back to front
        for (int idx = 0; idx < DisplayLayers.Count; idx++)
        {
            foreach (var (tileCoord,(tileId, firstFrameIdx, lastFrameIdx)) in DisplayLayers[^idx])
            {
                // DrawTile(tileCoord, tileId, mask);
                if(tileId < 0) return;
                int numFrames = lastFrameIdx - firstFrameIdx + 1;
                int frameIdx = ErMath.RoundToInt(ErEngine.CurrentTime * Map.TileData[tileId].Fps) % numFrames + firstFrameIdx;
                ErVec2 pos = (ErVec2)(Map.TileSize * tileCoord);
                Frames[frameIdx].Draw(pos);
            }
        }
    }
    private void AddTilemask(int x, int y, SwTileMask mask)
    {
        TileMasks[GetMaskIdx(x,y)] = mask;
    }
    private bool TryGetMask(int x, int y, out SwTileMask mask)
    {
        mask = TileMasks[GetMaskIdx(x%ATLAS_WIDTH, y%ATLAS_HEIGHT)];
        return mask != SwTileMask.None;
    }
    private static int GetMaskIdx(int x, int y)
    {
        return y * ATLAS_WIDTH + x;
    }
    private static bool IsSurfaceRectEmpty(nint surfaceHandle, ErRect2I rect)
    {
        for (int x = rect.Position.X; x < rect.Position.X + rect.Size.X; x++)
        {
            for (int y = rect.Position.Y; y < rect.Position.Y + rect.Size.Y; y++)
            {
                SDL.ReadSurfacePixel(surfaceHandle, x, y, out _, out _, out _, out byte a);
                if(a != 0) return false;
            }
        }
        return true;
    }
}
