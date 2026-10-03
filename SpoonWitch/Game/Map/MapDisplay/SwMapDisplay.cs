using Eris;
using Eris.Renderer;
using ErisMath;
using SDL3;
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
    private readonly struct DisplayTile
    {
        public int TileId{get; init;}
        public int FirstFrameIdx{get; init;}
        public int LastFrameIdx{get; init;}
    }
    private static readonly ErVec2I[] Neighbors = [ErVec2I.Zero, ErVec2I.Right, ErVec2I.Down, ErVec2I.One];
    private readonly SwMap Map;
    private readonly SwTileMask[] TileMasks;
    private const int ATLAS_WIDTH = 4;
    private const int ATLAS_HEIGHT = 4;
    private readonly List<Dictionary<ErVec2I,DisplayTile>> DisplayLayers = [];
    // tileId, mask, variant
    private readonly DisplayTile?[][][] TileAnimations;
    private readonly List<SwFrame> Frames = [];
    private readonly HashSet<ErVec2I> PendingDisplayTiles = [];
    public SwMapDisplay(SwMap map, int numDisplayLayers)
    {
        Map = map;
        for (int idx = 0; idx < numDisplayLayers; idx++)
        {
            DisplayLayers.Add([]);
        }
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
        TileAnimations = new DisplayTile?[Map.TileData.Length][][];
        foreach (var tileData in Map.TileData)
        {
            if(tileData.TextureFilepath is null) continue;
            if(!ErTexture.TryFromPath(tileData.TextureFilepath, out var texture, out nint surfaceHandle)) continue;
            SwTextureStore store = new(texture);
            int maxNumVariants = (int)texture.Size.X / (Map.TileSize.X * ATLAS_WIDTH);
            int maxNumAnimations = (int)texture.Size.Y / (Map.TileSize.X * ATLAS_HEIGHT);
            // reserve masks
            TileAnimations[tileData.Id] = new DisplayTile?[ATLAS_WIDTH * ATLAS_HEIGHT][];
            int tileX, tileY;
            for (int xi = 0; xi < ATLAS_WIDTH; xi++)
            {
                for (int yi = 0; yi < ATLAS_HEIGHT; yi++)
                {
                    int maskIdx = GetMaskIdx(xi, yi);
                    SwTileMask mask = TileMasks[maskIdx];
                    // reserve variants
                    List<DisplayTile> variants = [];
                    for (int varIdx = 0; varIdx < maxNumVariants; varIdx++)
                    {
                        tileX = xi + varIdx * ATLAS_WIDTH;
                        // get frames
                        int firstFrameIdx = Frames.Count;
                        int lastFrameIdx = firstFrameIdx - 1;
                        for (int frameIdx = 0; frameIdx < maxNumAnimations; frameIdx++)
                        {
                            tileY = yi + frameIdx * ATLAS_HEIGHT;
                            ErRect2I rect = new ErRect2I(tileX, tileY, 1, 1) * Map.TileSize;
                            if(IsSurfaceRectEmpty(surfaceHandle, rect)) continue;
                            SwFrame frame = new(store, (ErRect2)rect);
                            Frames.Add(frame);
                            lastFrameIdx++;
                        }
                        if(lastFrameIdx >= firstFrameIdx)
                        {
                            var tile = new DisplayTile
                            {
                                TileId = tileData.Id,
                                FirstFrameIdx = firstFrameIdx,
                                LastFrameIdx = lastFrameIdx,
                            };
                            variants.Add(tile);
                        }
                    }
                    TileAnimations[tileData.Id][(int)mask] = [..variants];
                }
            }
        }
    }
    public void Draw()
    {
        if(PendingDisplayTiles.Count > 0)
        {
            foreach (var displayCoord in PendingDisplayTiles)
            {
                UpdateDisplayTile(displayCoord);
            }
            PendingDisplayTiles.Clear();
        }
        var tileSize = (ErVec2)Map.TileSize;
        var half = tileSize / 2;
        // draw layers back to front
        for (int idx = 0; idx < DisplayLayers.Count; idx++)
        {
            foreach (var (tileCoord,displayTile) in DisplayLayers[DisplayLayers.Count - idx - 1])
            {
                int numFrames = displayTile.LastFrameIdx - displayTile.FirstFrameIdx + 1;
                int frameIdx = ErMath.RoundToInt(ErEngine.CurrentTime * Map.TileData[displayTile.TileId].Fps) % numFrames + displayTile.FirstFrameIdx;
                ErVec2 pos = (ErVec2)tileCoord * tileSize - half;
                Frames[frameIdx].Draw(pos);
            }
        }
    }
    public void QueueTileUpdate(ErVec2I tileCoord)
    {
        foreach (var n in Neighbors)
        {
            PendingDisplayTiles.Add(tileCoord + n);
        }
    }
    private void UpdateDisplayTile(ErVec2I displayCoord)
    {
        for (int layerIdx = 0; layerIdx < DisplayLayers.Count; layerIdx++)
        {
            int br = Map.GetTileId(layerIdx, displayCoord - Neighbors[0]);
            int bl = Map.GetTileId(layerIdx, displayCoord - Neighbors[1]);
            int tr = Map.GetTileId(layerIdx, displayCoord - Neighbors[2]);
            int tl = Map.GetTileId(layerIdx, displayCoord - Neighbors[3]);
            int tileId = br;
            if(bl > tileId) tileId = bl;
            if(tr > tileId) tileId = tr;
            if(tl > tileId) tileId = tl;
            if(tileId < 0 || !Map.TileData[tileId].IsVisible)
            {
                DisplayLayers[layerIdx].Remove(displayCoord);
                continue;
            }
            SwTileMask mask = SwTileMask.None;
            if(IsMatch(tileId, br)) mask |= SwTileMask.BottomRight;
            if(IsMatch(tileId, bl)) mask |= SwTileMask.BottomLeft;
            if(IsMatch(tileId, tr)) mask |= SwTileMask.TopRight;
            if(IsMatch(tileId, tl)) mask |= SwTileMask.TopLeft;
            var seed = (ushort)displayCoord.GetHashCode();
            var masks = TileAnimations[tileId];
            if(masks is null || masks.Length == 0){ErEngine.LogWarning("bad tile id ", tileId); return;}
            var variants = masks[(int)mask];
            if(variants is null || variants.Length == 0){ErEngine.LogWarning("bad mask, tile id: ", tileId, " mask: ", mask); return;}
            int varIdx = seed % variants.Length;
            var anim = variants[varIdx];
            if(anim is null){ErEngine.LogWarning("bad variant"); return;}
            DisplayTile displayTile = new()
            {
                TileId = tileId,
                FirstFrameIdx = anim.Value.FirstFrameIdx,
                LastFrameIdx = anim.Value.LastFrameIdx,
            };
            DisplayLayers[layerIdx][displayCoord] = displayTile;
        }
    }
    private static bool IsMatch(int tileId, int altId)
    {
        if(altId == -2) return true;
        if(altId < 0) return false;
        return tileId == altId;
    }
    private void AddTilemask(int x, int y, SwTileMask mask)
    {
        TileMasks[GetMaskIdx(x,y)] = mask;
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
