using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SDL3;

namespace SpoonWitch.Game.Map;

public enum SwTileMask: byte
{
    None = 0,
    TopLeft = 1,
    TopRight = 2,
    BottomLeft = 4,
    BottomRight = 8,
}

public class SwTileData
{
    public readonly ErTexture Texture;
    public readonly bool IsSolid;
    public readonly bool IsOpaque;
    public readonly double MoveSpeedMul;
    public readonly uint CollisionMask;
    public readonly bool IsAnimated;
    public const int ATLAS_WIDTH = 4;
    public const int ATLAS_HEIGHT = 4;
    public double Fps = 4;
    private readonly ErRect2[][][] Frames;
    private static readonly SwTileMask[] TileMasks;
    private static readonly ErVec2I[] CoordLookup;
    static SwTileData()
    {
        CoordLookup = new ErVec2I[ATLAS_WIDTH * ATLAS_HEIGHT];
        Array.Fill(CoordLookup, ErVec2I.Neg);
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
    }
    private static int GetMaskIdx(int x, int y)
    {
        return y * ATLAS_WIDTH + x;
    }
    private static void AddTilemask(int x, int y, SwTileMask mask)
    {
        CoordLookup[(int)mask] = new(x,y);
        TileMasks[GetMaskIdx(x,y)] = mask;
    }
    private static bool TryGetMask(int x, int y, out SwTileMask mask)
    {
        mask = TileMasks[GetMaskIdx(x%ATLAS_WIDTH, y%ATLAS_HEIGHT)];
        return mask != SwTileMask.None;
    }
    private static bool IsSurfaceRectEmpty(nint surface, ErRect2I rect)
    {
        for (int x = rect.Position.X; x < rect.Position.X + rect.Size.X; x++)
        {
            for (int y = rect.Position.Y; y < rect.Position.Y + rect.Size.Y; y++)
            {
                SDL.ReadSurfacePixel(surface, x, y, out _, out _, out _, out byte a);
                if(a != 0) return false;
            }
        }
        return true;
    }
    private SwTileData(string filepath, PriNode priNode, ErVec2I tileSize)
    {
        // TileSize = tileSize;
        if(!priNode.Get("source").TryAs(out string texPath)) throw new("no source field provided");
        string? dirpath = Path.GetDirectoryName(filepath);
        texPath = Path.Join(dirpath, texPath);
        if(!ErEngine.Renderer.TextureManager.TryGetSurface(texPath, out nint surface)) throw new("source path invalid");
        if(!ErTexture.TryFromPath(texPath, out Texture)) throw new("source path invalid2");
        IsSolid = priNode.TryGet("is_solid", out bool is_solid) && is_solid;
        IsOpaque = priNode.TryGet("is_opaque", out bool is_opaque) && is_opaque;
        MoveSpeedMul = priNode.TryGet("move_speed_mul", out double mul) ? mul : 1;
        IsAnimated = priNode.TryGet("is_animated", out bool is_animated) && is_animated;
        if (IsAnimated)
        {
            if(priNode.TryGet("fps", out double fps)) Fps = fps;
        }
        ErVec2I texSizeTiles = (ErVec2I)Texture.Size / tileSize;
        int numVariants = texSizeTiles.X / ATLAS_WIDTH;
        int numFrames = texSizeTiles.Y / ATLAS_HEIGHT;
        Frames = new ErRect2[numFrames][][];
        List<ErRect2> variants = new(numVariants);
        for (int frameIdx = 0; frameIdx < numFrames; frameIdx++)
        {
            var frame = new ErRect2[ATLAS_WIDTH * ATLAS_HEIGHT][];
            int yf = frameIdx * ATLAS_HEIGHT;
            for (int xi = 0; xi < ATLAS_WIDTH; xi++)
            {
                for (int yi = 0; yi < ATLAS_HEIGHT; yi++)
                {
                    variants.Clear();
                    if(!TryGetMask(xi, yi, out var mask)) continue;
                    for (int varIdx = 0; varIdx < numVariants; varIdx++)
                    {
                        int x = (xi + varIdx * ATLAS_WIDTH) * tileSize.X;
                        int y = (yi + yf) * tileSize.Y;
                        ErRect2I rect = new(x, y, tileSize.X, tileSize.Y);
                        if(IsSurfaceRectEmpty(surface, rect)) continue;
                        variants.Add((ErRect2)rect);
                    }
                    frame[(int)mask] = [..variants];
                }
            }
            for (int i = 0; i < frame.Length; i++)
            {
                if(frame[i] is null) frame[i] = [];
            }
            Frames[frameIdx] = frame;
        }
    }
    public bool TryDraw(ErVec2 position, SwTileMask mask, ushort seed)
    {
        return TryDraw(position, mask, seed, 0);
    }
    public bool TryDraw(ErVec2 position, SwTileMask mask, ushort seed, double time)
    {
        return TryDraw(position, mask, seed, ErMath.FloorToInt(time * Fps) % Frames.Length);
    }
    private bool TryDraw(ErVec2 position, SwTileMask mask, ushort seed, int frameIdx)
    {
        var frame = Frames[frameIdx];
        if(frame.Length == 0) return false;
        var variants = frame[(int)mask];
        if(variants.Length == 0) return false;
        var rect = variants[seed % variants.Length];
        Texture.Draw(position, rect.Size, rect);
        return true;
    }
    public static bool TryFromData(string filepath, PriNode priNode, ErVec2I tileSize, out SwTileData value)
    {
        value = default!;
        try
        {
            value = new(filepath, priNode, tileSize);
            return true;
        }
        catch(Exception e)
        {
            return ErEngine.LogWarning(e);
        }
    }
}