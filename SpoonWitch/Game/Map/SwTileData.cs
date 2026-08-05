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
    private readonly Dictionary<SwTileMask,List<ErVec2I>> CoordLookup = [];
    private static readonly Dictionary<(int,int), SwTileMask> MaskLookup = [];
    private static bool TryGetMask(ErVec2I tileCoords, out SwTileMask mask)
    {
        // Todo: make less stupid
        if(MaskLookup.Count == 0)
        {
            MaskLookup.Add((2,1), SwTileMask.TopLeft | SwTileMask.TopRight | SwTileMask.BottomLeft | SwTileMask.BottomRight); // All corners
            MaskLookup.Add((1,3), SwTileMask.BottomRight); // Outer bottom-right corner
            MaskLookup.Add((0,0), SwTileMask.BottomLeft); // Outer bottom-left corner
            MaskLookup.Add((0,2), SwTileMask.TopRight); // Outer top-right corner
            MaskLookup.Add((3,3), SwTileMask.TopLeft); // Outer top-left corner
            MaskLookup.Add((1,0), SwTileMask.TopRight | SwTileMask.BottomRight); // Right edge
            MaskLookup.Add((3,2), SwTileMask.TopLeft | SwTileMask.BottomLeft); // Left edge
            MaskLookup.Add((3,0), SwTileMask.BottomLeft | SwTileMask.BottomRight); // Bottom edge
            MaskLookup.Add((1,2), SwTileMask.TopLeft | SwTileMask.TopRight); // Top edge
            MaskLookup.Add((1,1), SwTileMask.TopRight | SwTileMask.BottomLeft | SwTileMask.BottomRight); // Inner bottom-right corner
            MaskLookup.Add((2,0), SwTileMask.TopLeft | SwTileMask.BottomLeft | SwTileMask.BottomRight); // Inner top-left corner
            MaskLookup.Add((2,2), SwTileMask.TopLeft | SwTileMask.TopRight | SwTileMask.BottomRight); // Inner top-right corner
            MaskLookup.Add((3,1), SwTileMask.TopLeft | SwTileMask.TopRight | SwTileMask.BottomLeft); // Inner top-left corner
            MaskLookup.Add((2,3), SwTileMask.TopRight | SwTileMask.BottomLeft); // Bottom-left top-right corners
            MaskLookup.Add((0,1), SwTileMask.TopLeft | SwTileMask.BottomRight); // Top-left down-right corners
        }
        return MaskLookup.TryGetValue((tileCoords.X%4,tileCoords.Y%4), out mask);
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
    private SwTileData(ErTexture texture, bool isSolid, bool isOpaque, double moveSpeedMul)
    {
        Texture = texture;
        IsSolid = isSolid;
        IsOpaque = isOpaque;
        MoveSpeedMul = moveSpeedMul;
        CollisionMask = 0;
        if(isSolid) CollisionMask |= 1;
        if(isOpaque) CollisionMask |= 2;
    }
    private void AddTile(ErVec2I tileCoords)
    {
        if(!TryGetMask(tileCoords, out var mask))
        {
            ErEngine.LogWarning("invalid tile coords: ", tileCoords);
            return;    
        }
        if(!CoordLookup.TryGetValue(mask, out var coords))
        {
            coords = [];
            CoordLookup[mask] = coords;
        }
        coords.Add(tileCoords);
    }
    public bool TryGetAtlasCoord(SwTileMask mask, out ErVec2I coord)
    {
        coord = new(2,1);
        if(!CoordLookup.TryGetValue(mask, out var options)) return ErEngine.LogWarning("bad mask: ", mask);
        int idx =  ErMath.FloorToInt(Random.Shared.NextSingle() * options.Count);
        coord = options[idx];
        return true;
    }
    public static bool TryFromData(string filepath, PriNode priNode, ErVec2I tileSize, out SwTileData value)
    {
        value = default!;
        if(!priNode.Get("source").TryAs(out string texPath)) return ErEngine.LogWarning("no source field provided");
        string? dirpath = Path.GetDirectoryName(filepath);
        texPath = Path.Join(dirpath, texPath);
        if(!ErEngine.Renderer.TextureManager.TryGetSurface(texPath, out nint surface)) return ErEngine.LogWarning("source path invalid");
        if(!ErTexture.TryFromPath(texPath, out var texture)) return ErEngine.LogWarning("source path invalid2");
        if(!priNode.Get("is_solid").TryAs(out bool isSolid)) isSolid = false;
        // ErEngine.Log("source: ", texPath);
        if(!priNode.Get("is_opaque").TryAs(out bool isOpaque)) isOpaque = false;
        if(!priNode.Get("move_speed_mul").TryAs(out double moveSpeedMul)) moveSpeedMul = 1;
        value = new(texture, isSolid, isOpaque, moveSpeedMul);
        // init coord lookup
        ErVec2I texSizeTiles = (ErVec2I)value.Texture.Size / tileSize;
        for (int xi = 0; xi < texSizeTiles.X; xi++)
        {
            for (int yi = 0; yi < 4; yi++)
            {
                // check if empty
                int x = xi * tileSize.X;
                int y = yi * tileSize.Y;
                ErRect2I rect = new()
                {
                    Position = new(x,y),
                    Size = tileSize,
                };
                if(IsSurfaceRectEmpty(surface, rect)) continue;
                value.AddTile(new(xi, yi));
            }
        }
        return true;
    }
}