using ErisMath;
using ErisPhysics2D.Collider;

namespace ErisPhysics2D;

internal class ErWorldCell
{
    public uint[] TileMasks;
    private readonly ErPhysicsWorld2D World;
    public readonly Dictionary<int,ErColliderArea> Areas = [];
    public readonly Dictionary<int,ErColliderBody> Bodies = [];
    public readonly ErVec2I CoordCells;
    public readonly ErRect2I RectTiles;
    public ErWorldCell(ErVec2I coordCells, ErPhysicsWorld2D world)
    {
        CoordCells = coordCells;
        World = world;
        RectTiles = new(CoordCells * World.CellSizeTiles, World.CellSizeTiles);
        TileMasks = new uint[World.CellSizeTiles.GetArea()];
        Array.Fill(TileMasks, uint.MaxValue);
    }
    private int GetIdx(ErVec2I tileCoord)
    {
        tileCoord -= RectTiles.Position;
        return tileCoord.Y * World.CellSizeTiles.X + tileCoord.X;
    }
    public uint GetTileMask(ErVec2I tileCoord)
    {
        // Todo: add check here?
        return TileMasks[GetIdx(tileCoord)];
    }
    public void SetTileMask(ErVec2I tileCoord, uint tileMask)
    {
        TileMasks[GetIdx(tileCoord)] = tileMask;
    }
}

// internal class ErWorldCell
// {
//     public int[] TileIds;
//     private readonly ErPhysicsWorld2D World;
//     public readonly Dictionary<int,ErColliderArea> Areas = [];
//     public readonly Dictionary<int,ErColliderBody> Bodies = [];
//     public readonly ErVec2I CoordCells;
//     public readonly ErVec2I CoordTiles;
//     public ErWorldCell(ErVec2I coordCells, ErPhysicsWorld2D world)
//     {
//         CoordCells = coordCells;
//         World = world;
//         CoordTiles = CoordCells * World.CellSizeTiles;
//         TileIds = new int[World.CellSizeTiles.GetArea()];
//         Array.Fill(TileIds, World.DefaultTileIdx);
//     }
//     private int GetIdx(ErVec2I tileCoord)
//     {
//         return tileCoord.Y * World.CellSizeTiles.X + tileCoord.X;
//     }
//     public int GetTileId(ErVec2I tileCoord)
//     {
//         tileCoord -= CoordTiles;
//         return TileIds[GetIdx(tileCoord)];
//     }
//     public void SetTileId(ErVec2I tileCoord, int tileId)
//     {
//         tileCoord -= CoordTiles;
//         TileIds[GetIdx(tileCoord)] = tileId;
//     }
// }
